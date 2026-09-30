using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FarmAI.Models;

namespace FarmAI.Services;

public sealed class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly PromptRepository _prompts;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        PromptRepository prompts,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _prompts = prompts;
        _logger = logger;

        // Tự quản lý timeout bằng CancellationTokenSource để phân biệt timeout OpenAI
        // với việc người dùng đóng trang/hủy request. HttpClient không tự timeout lần hai.
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    private string? ApiKey
    {
        get
        {
            var key = _configuration["OPENAI_API_KEY"]
                      ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }

    public bool IsExternalAiConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public Task<AiAnalysisResult> AnalyzeAsync(
        string task,
        string? question,
        string farmDataJson,
        CancellationToken cancellationToken = default) =>
        AnalyzeCoreAsync(_prompts.Get(), task, question, farmDataJson, cancellationToken);

    public Task<AiAnalysisResult> AnalyzeWithPromptAsync(
        string promptFile,
        string task,
        string? question,
        string farmDataJson,
        CancellationToken cancellationToken = default) =>
        AnalyzeCoreAsync(_prompts.GetByFile(promptFile), task, question, farmDataJson, cancellationToken);

    private async Task<AiAnalysisResult> AnalyzeCoreAsync(
        PromptConfig promptConfig,
        string task,
        string? question,
        string farmDataJson,
        CancellationToken cancellationToken)
    {
        var taskPrompt = promptConfig.Tasks.TryGetValue(task, out var p)
            ? p
            : promptConfig.Tasks.GetValueOrDefault("summary", "Tóm tắt dữ liệu trang trại.");

        var maxChars = Math.Clamp(_configuration.GetValue("OpenAI:MaxFarmDataChars", 8000), 2000, 20000);
        var safeFarmData = farmDataJson.Length <= maxChars
            ? farmDataJson
            : farmDataJson[..maxChars] + "\n[Dữ liệu đã được cắt bớt do quá dài]";

        var userPrompt = BuildUserPrompt(
            promptConfig.UserTemplate,
            task,
            taskPrompt,
            question,
            safeFarmData);

        if (string.IsNullOrWhiteSpace(ApiKey))
            return BuildFallback(task, question, safeFarmData,
                $"Chưa cấu hình OPENAI_API_KEY nên hệ thống dùng phân tích nội bộ. Prompt {promptConfig.Version} chưa được gửi tới model thật.");

        var model = _configuration["OPENAI_MODEL"]
                    ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
                    ?? _configuration["OpenAI:Model"]
                    ?? "gpt-5.6-luna";

        var maxOutputTokens = Math.Clamp(_configuration.GetValue("OpenAI:MaxOutputTokens", 700), 200, 1600);
        var timeoutSeconds = Math.Clamp(_configuration.GetValue("OpenAI:TimeoutSeconds", 18), 5, 60);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                model,
                instructions = promptConfig.System,
                input = userPrompt,
                store = false,
                max_output_tokens = maxOutputTokens,
                // FarmAI chỉ cần phân tích nghiệp vụ ngắn; tắt reasoning sâu để giảm độ trễ.
                reasoning = new { effort = "none" },
                text = new
                {
                    verbosity = "low",
                    format = new
                    {
                        type = "json_schema",
                        name = "farm_ai_analysis",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                title = new { type = "string" },
                                summary = new { type = "string" },
                                highlights = new { type = "array", items = new { type = "string" } },
                                reminders = new { type = "array", items = new { type = "string" } },
                                warnings = new { type = "array", items = new { type = "string" } }
                            },
                            required = new[] { "title", "summary", "highlights", "reminders", "warnings" }
                        }
                    }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            var responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI HTTP {Status}: {Body}", (int)response.StatusCode, TruncateForLog(responseBody));

                var reason = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized =>
                        "OpenAI từ chối API key (HTTP 401). Hãy kiểm tra OPENAI_API_KEY trong User Secrets.",
                    HttpStatusCode.Forbidden =>
                        "API key chưa có quyền dùng model/project này (HTTP 403).",
                    HttpStatusCode.TooManyRequests =>
                        "OpenAI đang giới hạn tần suất hoặc tài khoản đã hết quota (HTTP 429).",
                    HttpStatusCode.BadRequest =>
                        "OpenAI từ chối cấu hình yêu cầu (HTTP 400). Hãy kiểm tra model/API và Structured Output.",
                    HttpStatusCode.NotFound =>
                        "Model hoặc endpoint OpenAI không khả dụng với project hiện tại (HTTP 404).",
                    _ => $"OpenAI API trả về lỗi HTTP {(int)response.StatusCode}."
                };

                return BuildFallback(task, question, safeFarmData,
                    $"{reason} Đã chuyển sang phân tích nội bộ. Prompt {promptConfig.Version}.");
            }

            var outputText = ExtractOutputText(responseBody);
            if (string.IsNullOrWhiteSpace(outputText))
                return BuildFallback(task, question, safeFarmData,
                    $"AI trả về phản hồi rỗng. Đã dùng phân tích nội bộ. Prompt {promptConfig.Version}.");

            var parsed = ParseAnalysis(outputText);
            if (parsed is null)
                return BuildFallback(task, question, safeFarmData,
                    $"Phản hồi AI không đúng định dạng JSON. Đã dùng phân tích nội bộ. Prompt {promptConfig.Version}.");

            parsed.Provider = $"OpenAI · {model} · Prompt {promptConfig.Version}";
            parsed.UsedExternalAi = true;
            return parsed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BuildFallback(task, question, safeFarmData,
                $"OpenAI không phản hồi trong {timeoutSeconds} giây. Đã dùng phân tích nội bộ để trang không bị treo. Prompt {promptConfig.Version}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Không kết nối được OpenAI API.");
            return BuildFallback(task, question, safeFarmData,
                $"Không kết nối được AI API. Đã dùng phân tích nội bộ. Prompt {promptConfig.Version}.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Phản hồi AI không phải JSON hợp lệ.");
            return BuildFallback(task, question, safeFarmData,
                $"AI trả dữ liệu sai định dạng. Đã dùng phân tích nội bộ. Prompt {promptConfig.Version}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi AI không xác định.");
            return BuildFallback(task, question, safeFarmData,
                $"Có lỗi khi xử lý AI. Đã dùng phân tích nội bộ. Prompt {promptConfig.Version}.");
        }
    }

    private static string BuildUserPrompt(
        string template,
        string task,
        string taskPrompt,
        string? question,
        string farmData)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new InvalidOperationException("Thiếu userTemplate trong file prompt.");

        return template
            .Replace("{{task}}", task, StringComparison.Ordinal)
            .Replace("{{taskPrompt}}", taskPrompt, StringComparison.Ordinal)
            .Replace("{{question}}", string.IsNullOrWhiteSpace(question) ? "(không có)" : question, StringComparison.Ordinal)
            .Replace("{{farmData}}", farmData, StringComparison.Ordinal);
    }

    private static string ExtractOutputText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("output_text", out var direct) &&
            direct.ValueKind == JsonValueKind.String)
        {
            return direct.GetString() ?? string.Empty;
        }

        if (!doc.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    parts.Add(text.GetString() ?? string.Empty);
            }
        }

        return string.Join("\n", parts);
    }

    private static AiAnalysisResult? ParseAnalysis(string text)
    {
        try
        {
            text = text.Trim();
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLine = text.IndexOf('\n');
                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLine >= 0 && lastFence > firstLine)
                    text = text[(firstLine + 1)..lastFence].Trim();
            }

            var result = JsonSerializer.Deserialize<AiAnalysisResult>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null || string.IsNullOrWhiteSpace(result.Summary)) return null;
            result.Highlights ??= [];
            result.Reminders ??= [];
            result.Warnings ??= [];
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static AiAnalysisResult BuildFallback(string task, string? question, string farmDataJson, string reason)
    {
        try
        {
            using var doc = JsonDocument.Parse(farmDataJson);
            var root = doc.RootElement;

            var herds = CountArray(root, "herds");
            var care = CountArray(root, "careLogs");
            var careSchedules = CountArray(root, "careSchedules");
            var vaccines = CountArray(root, "vaccines");
            var growth = CountArray(root, "growth");

            var overdue = 0;
            var upcoming = 0;
            if (root.TryGetProperty("vaccines", out var vaccineArray) && vaccineArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in vaccineArray.EnumerateArray())
                {
                    var completed = v.TryGetProperty("CompletedDate", out var completedProp) && completedProp.ValueKind != JsonValueKind.Null;
                    if (completed) continue;
                    if (v.TryGetProperty("ScheduledDate", out var dateProp) && DateTime.TryParse(dateProp.ToString(), out var date))
                    {
                        if (date.Date < DateTime.Today) overdue++;
                        else if (date.Date <= DateTime.Today.AddDays(7)) upcoming++;
                    }
                }
            }

            var lowStock = new List<string>();
            if (root.TryGetProperty("feed", out var feedArray) && feedArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in feedArray.EnumerateArray())
                {
                    var q = f.TryGetProperty("Quantity", out var qp) && qp.TryGetDecimal(out var qv) ? qv : 0;
                    var min = f.TryGetProperty("MinimumStock", out var mp) && mp.TryGetDecimal(out var mv) ? mv : 0;
                    if (q <= min && f.TryGetProperty("Name", out var name)) lowStock.Add(name.GetString() ?? "Vật tư");
                }
            }

            var result = new AiAnalysisResult
            {
                Title = task switch
                {
                    "schedule" => "Nhắc lịch chăm sóc và tiêm phòng",
                    "growth" => "Phân tích tăng trưởng",
                    "risk" => "Điểm cần theo dõi",
                    _ => "Tóm tắt tình trạng đàn"
                },
                Summary = $"Dữ liệu hiện có gồm {herds} đàn, {care} nhật ký chăm sóc, {careSchedules} lịch chăm sóc, {vaccines} lịch vaccine và {growth} bản ghi tăng trưởng trong phạm vi gửi AI. {reason}",
                Highlights =
                [
                    $"Có {overdue} lịch vaccine quá hạn và {upcoming} lịch trong 7 ngày tới.",
                    lowStock.Count == 0
                        ? "Chưa phát hiện thức ăn chạm ngưỡng tồn kho tối thiểu trong dữ liệu gửi."
                        : $"Tồn kho cần chú ý: {string.Join(", ", lowStock.Take(3))}."
                ],
                Reminders =
                [
                    "Kiểm tra các lịch chăm sóc và vaccine đến hạn hoặc quá hạn, sau đó cập nhật trạng thái sau khi thực hiện.",
                    "Duy trì ghi nhận cân nặng, chăm sóc và tồn kho định kỳ để báo cáo có đủ dữ liệu."
                ],
                Warnings =
                [
                    "Kết quả này chỉ hỗ trợ quản lý, không dùng để chẩn đoán bệnh hoặc thay thế chỉ định của bác sĩ thú y."
                ],
                Provider = "Phân tích nội bộ (fallback)",
                UsedExternalAi = false
            };

            if (!string.IsNullOrWhiteSpace(question))
                result.Highlights.Add($"Yêu cầu bổ sung đã ghi nhận: {question}");

            return result;
        }
        catch
        {
            return new AiAnalysisResult
            {
                Title = "Kết quả phân tích",
                Summary = reason,
                Warnings = ["Không thể đọc dữ liệu hệ thống để tạo phân tích nội bộ."],
                Provider = "Phân tích nội bộ (fallback)",
                UsedExternalAi = false
            };
        }
    }

    private static int CountArray(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

    private static string TruncateForLog(string value) => value.Length <= 1000 ? value : value[..1000];
}
