using System.Text.Json;

namespace FarmAI.Services;

public sealed class PromptRepository
{
    private static readonly HashSet<string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "prompts.json",
        "prompts.v1.json",
        "prompts.v2.json"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly object _sync = new();
    private readonly Dictionary<string, PromptConfig> _cache = new(StringComparer.OrdinalIgnoreCase);

    public PromptRepository(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public PromptConfig Get() => GetByFile("prompts.json");

    public PromptConfig GetByFile(string fileName)
    {
        if (!AllowedFiles.Contains(fileName))
            throw new ArgumentException("Prompt file không được phép.", nameof(fileName));

        lock (_sync)
        {
            if (_cache.TryGetValue(fileName, out var cached))
                return cached;

            var path = Path.Combine(_environment.ContentRootPath, "Prompts", fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Không tìm thấy prompt file: {fileName}", path);

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<PromptConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException($"Không thể đọc Prompts/{fileName}.");

            _cache[fileName] = config;
            return config;
        }
    }

    public IReadOnlyList<(string FileName, PromptConfig Config)> GetExperimentVersions() =>
    [
        ("prompts.v1.json", GetByFile("prompts.v1.json")),
        ("prompts.v2.json", GetByFile("prompts.v2.json")),
        ("prompts.json", GetByFile("prompts.json"))
    ];
}

public sealed class PromptConfig
{
    public string Version { get; set; } = "v3";
    public string System { get; set; } = string.Empty;
    public string UserTemplate { get; set; } = string.Empty;
    public Dictionary<string, string> Tasks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
