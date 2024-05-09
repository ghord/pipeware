using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Pipeware.SourceImport
{
    [JsonConverter(typeof(FileRewriteConverter))]
    public class FileRewrite
    {
        public required string Path { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Alias { get; set; }

        public List<IImportRewriter> Rewriters { get; set; } = new List<IImportRewriter>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Namespace { get; set; }

        class FileRewriteConverter : JsonConverter<FileRewrite>
        {
            public override FileRewrite? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var result = new FileRewrite { Path = reader.GetString()! };
                    return result;
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var result = new FileRewrite { Path = string.Empty };

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {

                            switch (reader.GetString()!.ToLowerInvariant())
                            {
                                case "path":
                                    reader.Read();
                                    result.Path = reader.GetString()!;
                                    break;
                                case "namespace":
                                    reader.Read();
                                    result.Namespace = reader.GetString();
                                    break;
                                case "rewriters":
                                    reader.Read();
                                    var rewriters = JsonSerializer.Deserialize<List<IImportRewriter>>(ref reader, options);

                                    if (rewriters != null)
                                    {
                                        result.Rewriters = rewriters;
                                    }
                                    break;
                                case "alias":
                                    reader.Read();
                                    result.Alias = reader.GetString();
                                    break;

                                default:
                                    throw new JsonException("Cannot parse FileRewrite");

                            }

                        }
                    }

                    return result;
                }
                else
                {
                    throw new JsonException("Cannot parse FileRewrite");
                }
            }

            public override void Write(Utf8JsonWriter writer, FileRewrite value, JsonSerializerOptions options)
            {
                if (value.Rewriters.Count == 0 && value.Namespace is null && value.Alias is null)
                {
                    writer.WriteStringValue(value.Path);
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", value.Path);
                    if (value.Namespace != null)
                    {
                        writer.WriteString("namespace", value.Namespace);
                    }
                    if (value.Alias != null)
                    {
                        writer.WriteString("alias", value.Alias);
                    }
                    if (value.Rewriters.Any())
                    {
                        writer.WritePropertyName("rewriters");
                        JsonSerializer.Serialize(writer, value.Rewriters, options);
                    }
                    writer.WriteEndObject();
                }
            }
        }
    }

}
