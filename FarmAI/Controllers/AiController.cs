using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
public class AiController(
    AppDbContext db,
    IAiService aiService,
    FarmDataService farmDataService,
    PromptRepository promptRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new AiPageViewModel
        {
            ExternalAiConfigured = aiService.IsExternalAiConfigured,
            PromptVersion = promptRepository.Get().Version,
            Logs = await LoadLogs()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(AiPageViewModel model, CancellationToken cancellationToken)
    {
        if (!new[] { "summary", "schedule", "growth", "risk" }.Contains(model.Task))
            ModelState.AddModelError(nameof(model.Task), "Loại phân tích không hợp lệ.");

        if (!ModelState.IsValid)
        {
            model.ExternalAiConfigured = aiService.IsExternalAiConfigured;
            model.PromptVersion = promptRepository.Get().Version;
            model.Logs = await LoadLogs();
            return View("Index", model);
        }

        var farmDataJson = await farmDataService.BuildAiContextAsync(cancellationToken);
        var result = await aiService.AnalyzeAsync(model.Task, model.Question, farmDataJson, cancellationToken);

        await SaveLogAsync(model.Task, model.Question, result, cancellationToken);

        model.Result = result;
        model.ExternalAiConfigured = aiService.IsExternalAiConfigured;
        model.PromptVersion = promptRepository.Get().Version;
        model.Logs = await LoadLogs();
        return View("Index", model);
    }

    [HttpGet]
    public IActionResult PromptLab()
    {
        return View(new PromptLabViewModel
        {
            ExternalAiConfigured = aiService.IsExternalAiConfigured
        });
    }

    [HttpPost]
    public async Task<IActionResult> PromptLab(PromptLabViewModel model, CancellationToken cancellationToken)
    {
        if (!new[] { "summary", "schedule", "growth", "risk" }.Contains(model.Task))
            ModelState.AddModelError(nameof(model.Task), "Loại phân tích không hợp lệ.");

        model.ExternalAiConfigured = aiService.IsExternalAiConfigured;
        if (!ModelState.IsValid) return View(model);

        var farmDataJson = await farmDataService.BuildAiContextAsync(cancellationToken);

        // Chạy V1/V2/V3 song song để trang không phải chờ 3 lần timeout nối tiếp.
        // Chỉ phần gọi HTTP chạy song song; ghi SQLite vẫn thực hiện tuần tự vì DbContext không thread-safe.
        var experiments = promptRepository.GetExperimentVersions();
        var runTasks = experiments.Select(async experiment =>
        {
            var sw = Stopwatch.StartNew();
            var result = await aiService.AnalyzeWithPromptAsync(
                experiment.FileName,
                model.Task,
                model.Question,
                farmDataJson,
                cancellationToken);
            sw.Stop();

            return new PromptExperimentResult
            {
                Version = experiment.Config.Version,
                PromptFile = experiment.FileName,
                SystemPrompt = experiment.Config.System,
                Result = result,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };
        }).ToArray();

        var results = await Task.WhenAll(runTasks);
        model.Results.AddRange(results);

        foreach (var item in results)
            await SaveLogAsync($"prompt-lab-{item.Version}", model.Question, item.Result, cancellationToken);

        return View(model);
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> ClearLogs()
    {
        db.AiLogs.RemoveRange(db.AiLogs);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa nhật ký AI.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SaveLogAsync(string task, string? question, AiAnalysisResult result, CancellationToken cancellationToken)
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)) userId = parsedId;

        db.AiLogs.Add(new AiLog
        {
            AppUserId = userId,
            Task = task,
            Question = question ?? string.Empty,
            Response = JsonSerializer.Serialize(result),
            Provider = result.Provider,
            Success = true,
            Error = result.UsedExternalAi ? null : "External AI unavailable/not configured/error; fallback used.",
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<List<AiLog>> LoadLogs() => db.AiLogs.AsNoTracking().Include(x => x.AppUser)
        .OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync();
}
