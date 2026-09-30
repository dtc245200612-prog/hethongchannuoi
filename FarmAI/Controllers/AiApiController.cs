using System.Security.Claims;
using System.Text.Json;
using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmAI.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
[IgnoreAntiforgeryToken]
public class AiApiController(
    AppDbContext db,
    IAiService aiService,
    FarmDataService farmDataService) : ControllerBase
{
    [HttpPost("summarize-herd")]
    public async Task<ActionResult<AiAnalysisResult>> SummarizeHerd(
        [FromBody] AiApiRequest request,
        CancellationToken cancellationToken)
    {
        // Header tùy biến + không bật CORS giúp endpoint JSON này không nhận POST form cross-site thông thường.
        if (!string.Equals(Request.Headers["X-FarmAI-Request"].ToString(), "1", StringComparison.Ordinal))
            return BadRequest(new { error = "Thiếu X-FarmAI-Request header." });

        var allowedTasks = new[] { "summary", "schedule", "growth", "risk" };
        if (!allowedTasks.Contains(request.Task))
            return BadRequest(new { error = "Loại phân tích không hợp lệ." });

        var farmDataJson = await farmDataService.BuildAiContextAsync(cancellationToken);
        var result = await aiService.AnalyzeAsync(request.Task, request.Question, farmDataJson, cancellationToken);

        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)) userId = parsedId;

        db.AiLogs.Add(new AiLog
        {
            AppUserId = userId,
            Task = $"api-{request.Task}",
            Question = request.Question ?? string.Empty,
            Response = JsonSerializer.Serialize(result),
            Provider = result.Provider,
            Success = true,
            Error = result.UsedExternalAi ? null : "External AI unavailable/not configured/error; fallback used.",
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync(cancellationToken);

        return Ok(result);
    }
}
