using System.Text.Json.Serialization;

namespace Pipeware.SourceImport.Rewriters
{
    public class ParameterConstraint
    {
        public required string Parameter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Types { get; set; }
    }
}
