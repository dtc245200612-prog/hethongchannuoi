using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FarmAI.Services;

// Read-only chatbot adapter. API keys are read exclusively from backend configuration.
// No AI-controlled tools, SQL execution, or writes are made available to the model.
public class ChatModelClient(IHttpClientFactory factory, IConfiguration config, IWebHostEnvironment env)
{
    private static readonly SemaphoreSlim Slots = new(2, 2);
    public string Provider => (config["AI:ChatProvider"] ?? "OpenAI").Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? "OLLAMA" : "OPENAI";
    public string Model => !string.IsNullOrWhiteSpace(config["AI:ChatModel"])
        ? config["AI:ChatModel"]! : Provider == "OPENAI" ? "gpt-4.1-mini" : (config["AI:Model"] ?? "");
    public bool Configured => !string.IsNullOrWhiteSpace(Model) &&
        (Provider != "OPENAI" || !string.IsNullOrWhiteSpace(config["OPENAI_API_KEY"]));

    public async Task<ModelChatResult> Generate(string question, List<ChatTurn> history, ChatFarmContext context, CancellationToken abort)
    {
        var system = await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Prompts", "chat_system_v3.txt"), abort);
        var template = await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Prompts", "chat_user_template.txt"), abort);
        var user = template.Replace("{{QUESTION}}", question).Replace("{{SNAPSHOT}}", context.Json);
        var messages = new List<object> { new { role = "system", content = system } };
        foreach (var turn in history) messages.Add(new { role = turn.Role, content = turn.Content });
        messages.Add(new { role = "user", content = user });

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(abort);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        bool acquired = false;
        try
        {
            await Slots.WaitAsync(deadline.Token); acquired = true;
            using var http = factory.CreateClient("farm-chat");
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var request = BuildRequest(messages);
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (response.StatusCode is HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    if (attempt == 0)
                    {
                        var suggested = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(500);
                        await Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp(suggested.TotalMilliseconds, 500, 2000)), deadline.Token);
                        continue;
                    }
                    throw new ChatFailure(response.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 503,
                        "PROVIDER_BUSY", "OpenAI đang giới hạn tốc độ hoặc tạm bận. Hãy thử lại sau.");
                }
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    throw new ChatFailure(503, "INVALID_API_KEY", "Khóa OpenAI không hợp lệ hoặc không có quyền. Hãy cập nhật User Secrets.");
                if (response.StatusCode == HttpStatusCode.BadRequest)
                    throw new ChatFailure(502, "INVALID_MODEL_REQUEST", "Model hoặc cấu hình định dạng đầu ra không được hỗ trợ. Hãy kiểm tra AI:ChatModel.");
                if (!response.IsSuccessStatusCode)
                    throw new ChatFailure(502, "PROVIDER_REJECTED", "Dịch vụ AI từ chối yêu cầu.");
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var memory = new MemoryStream();
                var bytes = new byte[8192]; int read;
                while ((read = await stream.ReadAsync(bytes, deadline.Token)) > 0)
                {
                    if (memory.Length + read > 128000)
                        throw new ChatFailure(502, "RESPONSE_TOO_LARGE", "Phản hồi AI vượt giới hạn.");
                    memory.Write(bytes, 0, read);
                }
                memory.Position = 0;
                using var envelope = await JsonDocument.ParseAsync(memory, cancellationToken: deadline.Token);
                string? text;
                if (Provider == "OPENAI")
                {
                    var choices = envelope.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() != 1) throw new JsonException("Unexpected choices");
                    var choice = choices[0];
                    if (choice.GetProperty("finish_reason").GetString() != "stop")
                        throw new ChatFailure(502, "INCOMPLETE_RESPONSE", "AI chưa hoàn thành câu trả lời; hãy hỏi ngắn hơn.");
                    var message = choice.GetProperty("message");
                    if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind == JsonValueKind.String)
                        throw new ChatFailure(502, "MODEL_REFUSAL", "AI từ chối câu hỏi; hãy diễn đạt lại.");
                    text = message.GetProperty("content").GetString();
                }
                else text = envelope.RootElement.GetProperty("message").GetProperty("content").GetString();
                return ChatSafety.ValidateModelJson(text, context.Sources.Select(x => x.Id));
            }
            throw new ChatFailure(503, "PROVIDER_BUSY", "Dịch vụ AI tạm bận.");
        }
        catch (OperationCanceledException) when (!abort.IsCancellationRequested)
        { throw new ChatFailure(504, "AI_TIMEOUT", "AI mất hơn 30 giây để phản hồi."); }
        catch (HttpRequestException)
        { throw new ChatFailure(503, "MODEL_UNAVAILABLE", "Không thể kết nối OpenAI/Ollama."); }
        catch (JsonException)
        { throw new ChatFailure(502, "INVALID_ENVELOPE", "Phản hồi của nhà cung cấp không đúng cấu trúc."); }
        catch (InvalidOperationException)
        { throw new ChatFailure(502, "INVALID_ENVELOPE", "Phản hồi của nhà cung cấp thiếu dữ liệu."); }
        catch (KeyNotFoundException)
        { throw new ChatFailure(502, "INVALID_ENVELOPE", "Phản hồi của nhà cung cấp thiếu dữ liệu."); }
        finally { if (acquired) Slots.Release(); }
    }

    private HttpRequestMessage BuildRequest(List<object> messages)
    {
        if (Provider == "OPENAI")
        {
            var apiKey = config["OPENAI_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ChatFailure(503, "AI_NOT_CONFIGURED", "Chưa thiết lập khóa OpenAI trong User Secrets.");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                model = Model,
                messages,
                store = false,
                stream = false,
                max_completion_tokens = 1400,
                response_format = OpenAiSchemas.ChatResponseFormat
            });
            return request;
        }
        var root = config["AI:BaseUrl"];
        if (string.IsNullOrWhiteSpace(root)) root = "http://localhost:11434";
        if (!Uri.TryCreate(root, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == "https" || (uri.Scheme == "http" && (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "ollama"))))
            throw new ChatFailure(503, "BAD_PROVIDER_URL", "Ollama URL phải sử dụng HTTPS hoặc địa chỉ nội bộ.");
        var ollama = new HttpRequestMessage(HttpMethod.Post, root.TrimEnd('/') + "/api/chat");
        ollama.Content = JsonContent.Create(new { model = Model, messages, stream = false, format = "json", options = new { temperature = 0 } });
        return ollama;
    }
}
