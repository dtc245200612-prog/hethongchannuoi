using FarmAI.Models;

namespace FarmAI.Services;

public interface IAiService
{
    bool IsExternalAiConfigured { get; }

    Task<AiAnalysisResult> AnalyzeAsync(
        string task,
        string? question,
        string farmDataJson,
        CancellationToken cancellationToken = default);

    Task<AiAnalysisResult> AnalyzeWithPromptAsync(
        string promptFile,
        string task,
        string? question,
        string farmDataJson,
        CancellationToken cancellationToken = default);
}
