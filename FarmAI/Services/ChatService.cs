using System.Security.Claims;

namespace FarmAI.Services;

// No database write endpoint is exposed to the model; chat is advisory and read-only.
public class ChatService(ChatContextService farm,ChatModelClient model)
{
    public async Task<ChatReply> Ask(ClaimsPrincipal user,ChatAskRequest input,CancellationToken ct)
    {
        var question=ChatSafety.ValidateQuestion(input.Question);
        var history=ChatSafety.SafeHistory(input.History);
        if(ChatSafety.RequiresVeterinarian(question))
            return new ChatReply("Tôi không thể chẩn đoán, kê đơn hoặc đưa liều điều trị hay lịch tiêm chuyên môn. " +
                "Bạn hãy trao đổi với bác sĩ thú y, cung cấp triệu chứng và hồ sơ chăm sóc đã ghi nhận.",
                "SAFETY","rule",new(),new(){"Không phải hướng dẫn điều trị."},DateTime.UtcNow.ToString("O"));

        var context=await farm.Build(user,ct);
        if(!model.Configured)
            return RuleBased(question,context);
        var result=await model.Generate(question,history,context,ct);
        var map=context.Sources.ToDictionary(s=>s.Id,StringComparer.Ordinal);
        var sources=result.SourceIds!.Select(id=>map[id]).ToList();
        return new ChatReply(result.Answer!,model.Provider,model.Model,sources,
            result.Limitations!,DateTime.UtcNow.ToString("O"));
    }

    private static ChatReply RuleBased(string question,ChatFarmContext context)
    {
        var q=question.ToLowerInvariant();
        string answer;
        if(q.Contains("tiêm")||q.Contains("lịch")||q.Contains("nhắc"))
            answer=$"Trong phạm vi tài khoản của bạn hiện có {context.OpenPlans} kế hoạch tiêm chưa đóng. " +
                "Vào mục 'Lịch tiêm, cảnh báo' để xem ngày, người phụ trách và phần còn phải thực hiện.";
        else if(q.Contains("đàn")||q.Contains("số con")||q.Contains("bao nhiêu")||q.Contains("chuồng"))
            answer=$"Tài khoản của bạn đang được phép xem {context.HerdCount} đàn hoạt động với tổng {context.AnimalCount} con. " +
                "Mở 'Quản lý đàn' để xem danh sách chi tiết và lịch sử biến động.";
        else if(q.Contains("cân")||q.Contains("tăng trưởng")||q.Contains("adg"))
            answer="Để xem tăng trưởng, vào 'Cân đo, tăng trưởng', chọn đàn và đối chiếu các lần cân. " +
                "ADG chỉ tính khi có đủ mẫu, thời gian khác nhau và phương pháp có thể so sánh.";
        else if(q.Contains("thức ăn")||q.Contains("chăm sóc")||q.Contains("thuốc"))
            answer="Vào 'Nhật ký chăm sóc' để ghi lượng thức ăn đã cấp, thuốc hoặc hoạt động quan sát. " +
                "Nhật ký cần kiểm tra và xác nhận; không dùng trợ lý để quyết định liều thuốc.";
        else answer="Tôi có thể hướng dẫn tra cứu số lượng đàn, lịch tiêm, chăm sóc và tăng trưởng. " +
            "Hiện chưa kết nối mô hình AI, vì vậy tôi chỉ trả lời theo quy tắc có sẵn. " +
            "Để trò chuyện linh hoạt, hãy cấu hình Ollama hoặc OPENAI_API_KEY ở máy chủ.";
        return new ChatReply(answer,"RULE_BASED","deterministic",new(){context.Sources[0]},
            new(){"Chế độ quy tắc, không phải phản hồi từ mô hình AI.","Thống kê chỉ thuộc phạm vi được phép xem."},
            DateTime.UtcNow.ToString("O"));
    }
}
