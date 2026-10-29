using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmAI.Services;

// Chat history is only user-supplied conversational context, NEVER authorization evidence.
public record ChatTurn(string Role, string Content);
public record ChatAskRequest(string? Question, List<ChatTurn>? History);
public record ChatSource(string Id, string Label, string Url);
public record ChatReply(string Answer, string Provider, string Model, List<ChatSource> Sources,
    List<string> Limitations, string CreatedAt, string PromptVersion = "v3");
public sealed class ChatFailure(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
public class ModelChatResult
{
    [JsonPropertyName("answer")] public string? Answer { get; set; }
    [JsonPropertyName("source_ids")] public List<string>? SourceIds { get; set; }
    [JsonPropertyName("limitations")] public List<string>? Limitations { get; set; }
}
public static class ChatSafety
{
    public const string Advisory = "⚠ Khuyến nghị từ AI chỉ mang tính tham khảo, không thay thế cho bác sĩ thú y.";
    public const string PromptVersion = "v3";

    public static string ValidateQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length > 800 ||
            question.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t'))
            throw new ChatFailure(400, "INVALID_QUESTION", "Nhập câu hỏi từ 1 đến 800 ký tự.");
        return question.Trim();
    }

    public static List<ChatTurn> SafeHistory(List<ChatTurn>? supplied)
    {
        if (supplied == null) return new();
        if (supplied.Count > 12) throw new ChatFailure(400,"HISTORY_TOO_LONG","Hội thoại quá dài. Hãy bắt đầu cuộc trò chuyện mới.");
        if (supplied.Any(x => x == null || (x.Role != "user" && x.Role != "assistant") ||
            string.IsNullOrWhiteSpace(x.Content) || x.Content.Length > 1400))
            throw new ChatFailure(400,"INVALID_HISTORY","Lịch sử trò chuyện không hợp lệ.");
        return supplied;
    }

    public static ModelChatResult ValidateModelJson(string? json, IEnumerable<string> validSourceIds)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 20000)
            throw new ChatFailure(502,"EMPTY_OR_LARGE","Mô hình trả lời rỗng hoặc vượt giới hạn.");
        ModelChatResult value;
        try
        {
            using var document=JsonDocument.Parse(json);
            if(document.RootElement.ValueKind!=JsonValueKind.Object ||
               document.RootElement.EnumerateObject().Any(x=>x.Name is not ("answer" or "source_ids" or "limitations")))
                throw new JsonException();
            value = JsonSerializer.Deserialize<ModelChatResult>(json) ?? throw new JsonException();
        }
        catch (JsonException) { throw new ChatFailure(502,"INVALID_FORMAT","Mô hình trả kết quả không đúng định dạng JSON."); }
        if (string.IsNullOrWhiteSpace(value.Answer) || value.Answer.Length > 4000 ||
            value.SourceIds == null || value.SourceIds.Count == 0 || value.SourceIds.Count > 12 ||
            value.SourceIds.Any(x => string.IsNullOrWhiteSpace(x)) ||
            value.Limitations == null || value.Limitations.Count > 10 ||
            value.Limitations.Any(x => x == null || x.Length > 500))
            throw new ChatFailure(502,"INVALID_SCHEMA","Kết quả AI thiếu trường bắt buộc hoặc quá dài.");
        var allowed=validSourceIds.ToHashSet(StringComparer.Ordinal);
        if (value.SourceIds.Except(allowed,StringComparer.Ordinal).Any())
            throw new ChatFailure(502,"INVALID_SOURCE","AI tham chiếu nguồn không tồn tại trong dữ liệu được phép.");
        value.SourceIds=value.SourceIds.Distinct(StringComparer.Ordinal).ToList();
        return value;
    }

    public static bool RequiresVeterinarian(string question)
    {
        var q=question.ToLowerInvariant();
        // Protective routing; not presented as a comprehensive medical content classifier.
        return new[]{"kê đơn","liều thuốc","liều tiêm","chẩn đoán","phác đồ","tiêm bao nhiêu ml","cho uống bao nhiêu",
            "cách chữa bệnh","điều trị bệnh","tự pha thuốc"}.Any(q.Contains);
    }
}
