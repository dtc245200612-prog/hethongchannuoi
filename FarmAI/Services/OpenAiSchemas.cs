namespace FarmAI.Services;

// Strict Structured Output contract; validates syntax, then ChatSafety/ReportService checks source ids.
public static class OpenAiSchemas
{
    public static object ChatResponseFormat => new
    {
        type = "json_schema",
        json_schema = new
        {
            name = "farmai_chat_answer",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "answer", "source_ids", "limitations" },
                properties = new
                {
                    answer = new { type = "string" },
                    source_ids = new { type = "array", items = new { type = "string" } },
                    limitations = new { type = "array", items = new { type = "string" } }
                }
            }
        }
    };

    public static object ReportResponseFormat => new
    {
        type = "json_schema",
        json_schema = new
        {
            name = "farmai_report_draft",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "summary", "observations", "missing_data", "limitations" },
                properties = new
                {
                    summary = new { type = "string" },
                    observations = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "text", "source_ids" },
                            properties = new
                            {
                                text = new { type = "string" },
                                source_ids = new { type = "array", items = new { type = "string" } }
                            }
                        }
                    },
                    missing_data = new { type = "array", items = new { type = "string" } },
                    limitations = new { type = "array", items = new { type = "string" } }
                }
            }
        }
    };
}
