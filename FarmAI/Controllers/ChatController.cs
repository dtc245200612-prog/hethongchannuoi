using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FarmAI.Controllers;

// [Authorize] is inherited from AppController; anti-forgery is globally enforced on POST.
public class ChatController(ChatService chat,ChatModelClient model) : AppController
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.AiStatus=model.Configured?"Đã nhận cấu hình: "+model.Provider+" ("+model.Model+"). Kết nối sẽ được kiểm tra khi gửi câu hỏi.":
            "Chế độ quy tắc: cần cấu hình model để bật AI thật.";
        return View();
    }

    [HttpPost,EnableRateLimiting("ai")]
    [RequestSizeLimit(64_000)]
    public async Task<IActionResult> Ask([FromBody]ChatAskRequest? request,CancellationToken ct)
    {
        if(request==null)return BadRequest(new{code="INVALID_BODY",message="Yêu cầu trò chuyện không hợp lệ."});
        try{return Json(await chat.Ask(User,request,ct));}
        catch(ChatFailure ex){return StatusCode(ex.StatusCode,new{code=ex.Code,message=ex.Message});}
    }
}
