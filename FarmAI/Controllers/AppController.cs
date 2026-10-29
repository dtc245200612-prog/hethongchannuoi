using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public abstract class AppController : Controller
{
    protected async Task<IActionResult> Save(Func<Task> action,string returnUrl)
    {
        try { await action();TempData["Success"]="Thao tác đã được ghi nhận thành công."; }
        catch(BusinessException ex){TempData["Error"]=ex.Message;}
        catch(DbUpdateConcurrencyException){TempData["Error"]="Dữ liệu đã thay đổi. Vui lòng tải lại trang.";}
        catch(DbUpdateException){TempData["Error"]="Dữ liệu trùng hoặc vi phạm ràng buộc; không có thay đổi nào được xác nhận.";}
        return LocalRedirect(returnUrl);
    }
}
