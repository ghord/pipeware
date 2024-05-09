using System.Text.Json.Serialization;

namespace Pipeware.SourceImport.Rewriters
{
    public class GenericConstraint
    {
        public required string Parameter { get; set; }

        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Class { get; set; }
    }
}
