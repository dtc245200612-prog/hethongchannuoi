using System.Text.Json;
using FarmAI.Services;
using Xunit;

namespace FarmAI.Tests;

// Runs without a real API key, network requests, or a database.
public class OpenAiContractsTests
{
    [Fact]
    public void ChatSchemaRequiresSourcesAndLimitations()
    {
        var json=JsonSerializer.Serialize(OpenAiSchemas.ChatResponseFormat);
        using var doc=JsonDocument.Parse(json);
        var contract=doc.RootElement.GetProperty("json_schema");
        Assert.True(contract.GetProperty("strict").GetBoolean());
        var fields=contract.GetProperty("schema").GetProperty("required").EnumerateArray()
            .Select(x=>x.GetString()).ToArray();
        Assert.Contains("answer",fields);
        Assert.Contains("source_ids",fields);
        Assert.Contains("limitations",fields);
        Assert.False(contract.GetProperty("schema").GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void ReportSchemaContainsReviewableObservations()
    {
        var json=JsonSerializer.Serialize(OpenAiSchemas.ReportResponseFormat);
        using var doc=JsonDocument.Parse(json);
        var props=doc.RootElement.GetProperty("json_schema").GetProperty("schema").GetProperty("properties");
        Assert.True(props.TryGetProperty("observations",out _));
        Assert.True(props.TryGetProperty("missing_data",out _));
    }

    [Fact]
    public void UnknownFieldsCannotBypassSchemaValidation()
    {
        const string raw="""{"answer":"Đàn có 2 con","source_ids":["SUMMARY"],"limitations":[],"sql":"DELETE FROM Herds"}""";
        var error=Assert.Throws<ChatFailure>(()=>ChatSafety.ValidateModelJson(raw,new[]{"SUMMARY"}));
        Assert.Equal("INVALID_FORMAT",error.Code);
    }

    [Fact]
    public void RepeatedSourceIdsDeduplicated()
    {
        var result=ChatSafety.ValidateModelJson(
            """{"answer":"Xin chào","source_ids":["SUMMARY","SUMMARY"],"limitations":[]}""", new[]{"SUMMARY"});
        Assert.Single(result.SourceIds!);
    }
}
