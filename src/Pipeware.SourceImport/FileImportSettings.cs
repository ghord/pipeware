namespace Pipeware.SourceImport
{
    public class FileImportSettings
    {
        public required string SourceNamespace { get; set; }
        public required string SourcePath { get; set; }
        public required string? SourceType { get; set; }
        public required string SourceHash { get; set; }
        public required string TargetNamespace { get; set; }
        public required string TargetPath { get; set; }
        public required string? Alias { get; set; }
    }

}
