using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FarmAI.Services;

// Creates human-reviewable AI drafts from an immutable snapshot; no AI-originated writes to farm records.
public class OpenAiReportClient(IHttpClientFactory clients, IConfiguration settings, IWebHostEnvironment env)
{
    public bool Enabled => (settings["AI:ReportProvider"] ?? "OpenAI") == "OpenAI" &&
        !string.IsNullOrWhiteSpace(settings["OPENAI_API_KEY"]);
    public string Model => settings["AI:ReportModel"] ?? "gpt-4.1-mini";

    public async Task<AiContent> Generate(string task, object snapshot, IEnumerable<string> permitted, IEnumerable<string> missing)
    {
        if (!Enabled) throw new BusinessException("Chưa cấu hình OpenAI API key trên backend.");
        var prompt = await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Prompts", "report_system_v1.txt"));
        var payload = JsonSerializer.Serialize(new { task, snapshot });
        if (payload.Length > 50000) throw new BusinessException("Dữ liệu báo cáo quá lớn.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings["OPENAI_API_KEY"]);
        request.Content = JsonContent.Create(new
        {
            model = Model,
            messages = new object[] { new { role = "system", content = prompt }, new { role = "user", content = payload } },
            store = false, stream = false, max_completion_tokens = 1800,
            response_format = OpenAiSchemas.ReportResponseFormat
        });
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var response = await clients.CreateClient("farm-chat").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                throw new BusinessException("OpenAI đang bận hoặc đã hết hạn mức; thử lại sau.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new BusinessException("Khóa API OpenAI không hợp lệ hoặc không có quyền sử dụng model.");
            if (!response.IsSuccessStatusCode)
                throw new BusinessException("OpenAI từ chối yêu cầu; kiểm tra tên model hoặc quyền truy cập.");
            // Cap the provider envelope before JSON parsing.
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192]; int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + read > 160000) throw new BusinessException("Phản hồi báo cáo AI quá dài.");
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            using var json = await JsonDocument.ParseAsync(buffer, cancellationToken: timeout.Token);
            var choices = json.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() != 1 || choices[0].GetProperty("finish_reason").GetString() != "stop")
                throw new BusinessException("AI chưa hoàn thành báo cáo.");
            var raw = choices[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 40000) throw new BusinessException("AI trả phản hồi rỗng hoặc quá dài.");
            var report = JsonSerializer.Deserialize<AiContent>(raw) ?? throw new JsonException();
            var allowed = permitted.ToHashSet(StringComparer.Ordinal);
            var needed = missing.ToHashSet(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(report.summary) || report.summary.Length > 4000 ||
                report.observations == null || report.observations.Count > 30 ||
                report.missing_data == null || report.limitations == null ||
                report.observations.Any(x => x == null || string.IsNullOrWhiteSpace(x.text) || x.text.Length > 2500 ||
                    x.source_ids == null || x.source_ids.Count == 0 || x.source_ids.Except(allowed).Any()) ||
                needed.Except(report.missing_data).Any() || report.limitations.Count > 12)
                throw new BusinessException("Báo cáo AI không đáp ứng kiểm tra nguồn hoặc dữ liệu thiếu.");
            return report;
        }
        catch (OperationCanceledException) { throw new BusinessException("OpenAI mất hơn 30 giây để tạo báo cáo."); }
        catch (HttpRequestException) { throw new BusinessException("Không thể kết nối tới OpenAI."); }
        catch (JsonException) { throw new BusinessException("AI trả báo cáo JSON không hợp lệ."); }
        catch (KeyNotFoundException) { throw new BusinessException("AI trả báo cáo thiếu thông tin."); }
        catch (InvalidOperationException) { throw new BusinessException("AI trả báo cáo sai cấu trúc."); }
    }
}
