using Pipeware.SourceImport.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Pipeware.SourceImport
{
    [Description("Imports file from git repository")]
    public sealed partial class ImportCommand : Command<ImportCommand.ImportSettings>
    {
        private ILogger _logger = null!;
        public sealed class ImportSettings : CommandSettings
        {
            [Description("Folder of repository from which to import source")]
            [CommandOption("-s|--src|--source")]
            [DefaultValue("aspnetcore")]
            public required string SourceDirectory { get; init; }

            [Description("Pattern which files to consider for import")]
            [CommandOption("-p|--pattern")]
            public string? SourcePattern { get; init; }

            [Description("Branch (on remote) which we are importing from")]
            [CommandOption("-b|--branch")]
            [DefaultValue("release/8.0")]
            public required string SourceBranch { get; set; }

            [Description("Output directory into which we will generate files")]
            [CommandArgument(0, "<TARGET_DIRECTORY>")]
            public required string TargetDirectory { get; set; }

            [Description("Target namespace for file")]
            [CommandOption("-n|--namespace")]
            public string? TargetNamespace { get; init; }

            [Description("Base namespace for all files")]
            [CommandOption("--base-namespace")]
            [DefaultValue("Pipeware")]
            public required string BaseNamespace { get; init; }

            [Description("Reimport one of already imported files")]
            [CommandOption("-r|--reimport")]
            public bool Reimport { get; init; }

            [Description("Reimport all already imported files")]
            [CommandOption("-a|--all")]
            public bool All { get; init; }

            [Description("Remote location prefix to use when generating comments.")]
            [CommandOption("--remote-prefix")]
            [DefaultValue("https://github.com/dotnet/aspnetcore/tree")]
            public required string RemotePrefix { get; init; }

            [Description("If specified with reimport, will reimport most recently updated <N> files")]
            [CommandOption("--last")]
            public int? Last { get; init; }

            [Description("If specified, will output debug info")]
            [CommandOption("-v|--verbose")]
            public bool Verbose { get; init; }
        }

        public override int Execute(CommandContext context, ImportSettings settings)
        {
            _logger = new SpectreCliLogger(settings.Verbose);

            // check source
            if (!GitUtils.ContainsRepository(settings.SourceDirectory))
            {
                AnsiConsole.MarkupLine($"Directory '[teal]{settings.SourceDirectory}[/]' does not contain git repository, use [green]fetch[/] command");
                return -1;
            }

            // check target
            if (!Directory.Exists(settings.TargetDirectory))
            {
                AnsiConsole.MarkupLine($"Directory '[teal]{settings.TargetDirectory}[/]' does not exist");
                return -1;
            }

            // load settings from json
            var projectImportSettings = GetProjectImportSettings(settings);

            // get resource files in repo
            var resourceFiles = GetResourceFilesInRepository(settings);

            // select file we want to import
            Parallel.ForEach(SelectFilesForImport(settings, projectImportSettings), (item) =>
            {
                // import file
                ImportFile(settings, projectImportSettings, item.path, item.hash, item.alias, resourceFiles);
            });

            // save modified settings to project
            SaveProjectImportSettings(settings, projectImportSettings);

            return 0;
        }

        private void SaveProjectImportSettings(ImportSettings settings, ProjectImportSettings projectSettings)
        {
            try
            {
                var jsonPath = Path.Combine(settings.TargetDirectory, "SourceImport.json");
                using var file = File.Create(jsonPath);
                JsonSerializer.Serialize(file, projectSettings, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException ex)
            {
                AnsiConsole.Markup($"Cannot save import settings: [red]{ex.Message}[/]");
                AnsiConsole.WriteException(ex);

                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"Cannot save import settings: [red]{ex.Message}[/]");
                AnsiConsole.WriteException(ex);

                Environment.Exit(-1);
            }
        }

        private ProjectImportSettings GetProjectImportSettings(ImportSettings settings)
        {
            try
            {
                var jsonPath = Path.Combine(Path.GetFullPath(settings.TargetDirectory), "SourceImport.json");

                if (!Path.Exists(jsonPath))
                {
                    _logger.LogWarning("Project settings not found at [red]{jsonPath}[/]", jsonPath);
                    return new ProjectImportSettings();
                }
                else
                {
                    using var file = File.OpenRead(jsonPath);
                    var result = JsonSerializer.Deserialize<ProjectImportSettings>(file, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ProjectImportSettings();

                    _logger.LogInformation("Imported project settings from [teal]{jsonPath}[/]", jsonPath);

                    return result;
                }
            }
            catch (FileNotFoundException)
            {
                return new ProjectImportSettings();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cannot read import settings: [red]{message}[/]", ex.Message);

                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot read import settings: [red]{message}[/]", ex.Message);

                Environment.Exit(-1);
            }

            return new ProjectImportSettings();
        }

        private void ImportFile(ImportSettings settings, ProjectImportSettings projectSettings, string sourceFile, string sourceHash, string? alias, ResourceFileCache resourceFiles)
        {
            using var _ = _logger.BeginScope("Importing file '[teal]{sourceFile}[/]...", sourceFile);

            // get file content
            GitUtils.SetStandardOutputCapture(true);
            GitUtils.SetDirectory(settings.SourceDirectory);

            var result = GitUtils.RunCommand(_logger, "show", "origin/" + settings.SourceBranch + ":" + sourceFile);

            if (!result.Success)
            {
                _logger.LogError("Failed to read '{sourceFile}'", sourceFile);
                return;
            }

            // parse file and grab file import settings
            var parseTree = CSharpSyntaxTree.ParseText(string.Join("\r\n", result.StandardOutput!));
            var walker = new InformationExtractingSyntaxWalker(sourceFile);

            walker.Visit(parseTree.GetRoot());

            if (walker.Namespace == null)
            {
                _logger.LogError($"Cannot get namespace for file '[red]{sourceFile}[/]'");
                return;
            }

            var fileSettings = new FileImportSettings
            {
                SourceNamespace = walker.Namespace,
                SourcePath = sourceFile,
                SourceHash = sourceHash,
                SourceType = walker.Type,
                TargetNamespace = string.Empty,
                TargetPath = string.Empty,
                Alias = alias
            };

            // update project import settings based on inputs/user selected actions
            if (!SetTargetFileImportSettings(settings, projectSettings, fileSettings))
            {
                _logger.LogError("Cannot set target path");
                return;
            }

            var context = new RewriterContext(_logger, fileSettings.Alias);
            var rewriters = GetRewriters(settings, projectSettings, fileSettings, resourceFiles).ToArray();

            parseTree = parseTree.WithFilePath(fileSettings.TargetPath);

            // run all rewriters and save result
            foreach (var rewriter in rewriters)
            {
                if (string.IsNullOrEmpty(parseTree.FilePath))
                    throw new InvalidOperationException("Missing file path");

                parseTree = rewriter.Rewrite(context, parseTree);
            }

            File.WriteAllText(parseTree.FilePath, parseTree.GetText().ToString());

            _logger.LogInformation($"File imported -> '[green]{parseTree.FilePath}[/]'");
        }

        private string? GetDefaultTargetPath(ImportSettings settings, string sourcePath, string targetNamespace)
        {
            if (!targetNamespace.StartsWith(settings.BaseNamespace))
            {
                _logger.LogInformation("Target namespace {targetNamespace} does not start with '{namespace}', cannot get target path", targetNamespace, settings.BaseNamespace);
                return null;
            }

            var parts = targetNamespace.Substring(settings.BaseNamespace.Length).Split('.', StringSplitOptions.RemoveEmptyEntries);

            var targetPath = Path.Combine([Path.GetFullPath(settings.TargetDirectory), .. parts, Path.GetFileName(sourcePath)]);
            var targetDirectoryPath = Path.GetDirectoryName(targetPath)!;

            if (!Directory.Exists(targetDirectoryPath))
            {
                _logger.LogInformation("Creating directory [teal]{targetDirectoryPath}[/] for namespace [teal]{targetNamespace}[/]", targetDirectoryPath, targetNamespace);
                Directory.CreateDirectory(targetDirectoryPath);
            }


            return targetPath;
        }

        private bool SetTargetFileImportSettings(ImportSettings settings, ProjectImportSettings projectSettings, FileImportSettings fileSettings)
        {
            if (projectSettings.Files.FirstOrDefault(f => f.Path.Equals(fileSettings.SourcePath)) is FileRewrite fileRewrite && fileRewrite.Namespace is not null)
            {
                // if we have already an override in SourceImport.Json, use it
                var targetNamespace = fileRewrite.Namespace;

                if (!settings.Reimport)
                {
                    targetNamespace = settings.TargetNamespace ?? AnsiConsole.Ask($"Target namespace from [yellow]{fileSettings.SourceNamespace}[/]?[/]", targetNamespace);
                }

                fileSettings.TargetNamespace = targetNamespace;
            }
            else if (!projectSettings.Namespaces.TryGetValue(fileSettings.SourceNamespace, out var targetNamespace))
            {
                // if we've never encountered this namespace before, ask user for mapping
                targetNamespace = settings.TargetNamespace ?? AnsiConsole.Ask<string>($"[white]Target namespace from [yellow]{fileSettings.SourceNamespace}[/]?[/]");

                if (AnsiConsole.Confirm($"[white]Add default namespace mapping from [teal]{fileSettings.SourceNamespace}[/] to [green]{targetNamespace}[/]?[/]"))
                {
                    projectSettings.Namespaces.Add(fileSettings.SourceNamespace, targetNamespace);
                }

                fileSettings.TargetNamespace = targetNamespace;
            }
            else
            {
                // if we've encountered it before
                if (!settings.Reimport)
                {
                    var finalNamespace = settings.TargetNamespace ?? AnsiConsole.Ask($"[white]Target namespace from [yellow]{fileSettings.SourceNamespace}[/]?[/]", targetNamespace);

                    if (finalNamespace != targetNamespace)
                    {
                        if (AnsiConsole.Confirm($"[white]Change default namespace mapping from [teal]{fileSettings.SourceNamespace}->{targetNamespace}[/] to [green]{finalNamespace}[/]?[/]"))
                        {
                            projectSettings.Namespaces[fileSettings.SourceNamespace] = finalNamespace;
                        }
                    }

                    targetNamespace = finalNamespace;
                }


                fileSettings.TargetNamespace = targetNamespace;
            }

            if (fileSettings.SourceType != null)
            {
                var targetType = fileSettings.SourceType;
                if (!settings.Reimport)
                {
                    targetType = AnsiConsole.Ask("[white]Target type name[/]", targetType);

                    if (targetType != fileSettings.SourceType)
                    {
                        var defaultRule = projectSettings.Rules.FirstOrDefault(r => r.IsDefault);

                        if (defaultRule == null)
                        {
                            projectSettings.Rules.Add(defaultRule = RewriteRule.CreateDefault());
                        }

                        var typeRewriter = defaultRule.Rewriters.OfType<RenameTypeRewriter>().FirstOrDefault(r => r.IsDefault);

                        if (typeRewriter == null)
                        {
                            defaultRule.Rewriters.Add(typeRewriter = RenameTypeRewriter.CreateDefault());
                        }

                        typeRewriter.AddRename(fileSettings.SourceType, targetType);
                    }
                }
            }

            if (!projectSettings.Files.Any(f => f.Path.Equals(fileSettings.SourcePath)))
            {
                var rewriteRule = new FileRewrite { Path = fileSettings.SourcePath };

                if (!projectSettings.Namespaces.TryGetValue(fileSettings.SourceNamespace, out var targetNamespace) || !targetNamespace.Equals(fileSettings.TargetNamespace))
                {
                    rewriteRule.Namespace = fileSettings.TargetNamespace;
                }

                projectSettings.Files.Add(rewriteRule);
                projectSettings.Files.Sort((lhs, rhs) => lhs.Path.CompareTo(rhs.Path));
            }

            if (string.IsNullOrEmpty(fileSettings.TargetPath))
            {
                if (GetDefaultTargetPath(settings, fileSettings.SourcePath, fileSettings.TargetNamespace) is string targetPath)
                {

                    fileSettings.TargetPath = targetPath;
                    return true;
                }

                return false;
            }

            return true;
        }



        private IEnumerable<IImportRewriter> GetRewriters(ImportSettings settings, ProjectImportSettings projectSettings, FileImportSettings fileSettings, ResourceFileCache resourceFiles)
        {
            yield return new NamespaceDeclarationRewriter { TargetNamespace = fileSettings.TargetNamespace };

            foreach (var (source, target) in projectSettings.Namespaces)
            {
                yield return new NamespaceRewriter { SourceNamespace = source, TargetNamespace = target };
            }

            var remoteFileLocation = settings.RemotePrefix + "/" + settings.SourceBranch + "/" + fileSettings.SourcePath;

            yield return new LicenseRewriter(fileSettings.SourceHash, remoteFileLocation, fileSettings.Alias);

            var resourceFile = SelectResourceFile(fileSettings.SourcePath, resourceFiles.Files);

            if (resourceFile != null)
            {
                var resources = resourceFiles.GetTextResources(resourceFile);

                yield return new ResourceRewriter(resources);
            }

            var fileMappings = projectSettings.Files.Where(f => f.Path.Equals(fileSettings.SourcePath)).ToArray();
            FileRewrite? fileMapping = null;

            if (fileMappings.Length > 1)
            {
                //try to disambiguate based on type
                var mapping = fileMappings.SingleOrDefault(m => m.Alias == fileSettings.Alias);


                if (mapping == null)
                    throw new Exception("Ambiguous file mapping. Use aliases if you wish to import the same file multiple times.");

                fileMapping = mapping;
            }
            else
            {
                fileMapping = fileMappings[0];
            }

            var fileRewriters = fileMapping?.Rewriters ?? [];

            foreach (var rewriter in fileRewriters)
            {
                yield return rewriter;
            }

            foreach (var rule in projectSettings.Rules)
            {
                if (string.IsNullOrEmpty(rule.Include) && rule.Includes is null)
                    continue;

                if (!rule.MatchesPath(fileSettings.SourcePath))
                    continue;

                if (!rule.MatchesAlias(fileSettings.Alias))
                    continue;

                foreach (var rewriter in rule.Rewriters)
                {
                    yield return rewriter;
                }
            }
        }

        private string? SelectResourceFile(string sourcePath, IEnumerable<string> resourceFiles)
        {

            // Normalize directory paths and filter out non-ancestors
            var ancestors = resourceFiles
                .Select(file => Path.GetDirectoryName(file)!.Replace(Path.DirectorySeparatorChar, '/') + '/') // Normalize path
                .Where(dirPath => sourcePath.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Sort by depth (number of directory separators) in descending order
            var closestAncestor = ancestors
                .Where(d => d is not null)
                .OrderByDescending(dirPath => dirPath!.Count(c => c == '/'))
                .FirstOrDefault();

            if (closestAncestor == null)
            {
                _logger.LogDebug(".resx file not found for file [teal]{sourcePath}[/]", sourcePath);

                return null;
            }
            else
            {
                var resourceFile = resourceFiles.First(r => r.StartsWith(closestAncestor));

                _logger.LogDebug("Loading [teal]{resourceFile}[/] for resource lookup", resourceFile);

                return resourceFile;
            }
        }

        private string[] GetFilesInRepository(ImportSettings settings)
        {
            // grab files that match
            GitUtils.SetStandardOutputCapture(true);
            GitUtils.SetDirectory(settings.SourceDirectory);

            var result = GitUtils.RunCommand(_logger, "ls-tree", "-r", "origin/" + settings.SourceBranch);

            if (!result.Success)
            {
                AnsiConsole.MarkupLine("ls-tree command [red]failed[/]: ");

                foreach (var line in result.StandardError!)
                {
                    AnsiConsole.MarkupLine(line);
                }

                Environment.Exit(-1);
            }

            return result.StandardOutput!;
        }

        private ResourceFileCache GetResourceFilesInRepository(ImportSettings settings)
        {
            var files = new List<string>();
            var lsTreeOutput = GetFilesInRepository(settings);

            foreach (var line in lsTreeOutput)
            {
                var parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                    continue;

                if (!parts[1].Equals("blob"))
                    continue;

                if (GitUtils.MatchPattern("**/*.resx", parts[3]))
                {
                    files.Add(parts[3]);
                }
            }

            return new ResourceFileCache(files, _logger, settings);
        }

        private (string path, string? alias, string hash)[] SelectFilesForImport(ImportSettings settings, ProjectImportSettings projectSettings)
        {
            var lsTreeOutput = GetFilesInRepository(settings);

            var choices = new List<(string path, string? alias, string hash)>();


            if (settings.Reimport)
            {
                var importedFiles = new HashSet<string>(projectSettings.Files.Select(f => f.Path));
                var aliasLookup = projectSettings.Files.ToLookup(f => f.Path, f => f.Alias);

                foreach (var line in lsTreeOutput)
                {
                    var parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 4)
                        continue;

                    if (!parts[1].Equals("blob"))
                        continue;

                    if (!importedFiles.Contains(parts[3]))
                        continue;

                    var aliases = new HashSet<string?>();
                    bool hasNullAlias = false;

                    foreach (var alias in aliasLookup[parts[3]])
                    {
                        if (alias is not null && !aliases.Add(alias))
                            throw new Exception("Duplicate alias for path " + parts[3]);
                        else if (alias is null)
                        {
                            if (hasNullAlias)
                                throw new Exception("Duplicate non-aliased path " + parts[3]);

                            hasNullAlias = true;
                        }

                        choices.Add((parts[3], alias, parts[2]));
                    }
                }

                if (settings.Last is not null)
                {
                    var dates = new Dictionary<(string sourceFile, string? alias), DateTime>();

                    foreach (var codeFile in Directory.GetFiles(settings.TargetDirectory, "*.cs", SearchOption.AllDirectories))
                    {
                        if (LicenseRewriter.TryExtractInformation(_logger, codeFile, out var sourceFile, out _, out var alias))
                        {
                            if (!sourceFile.StartsWith(settings.RemotePrefix))
                            {
                                continue;
                            }

                            sourceFile = sourceFile.Substring(settings.RemotePrefix.Length + 1);

                            if (!sourceFile.StartsWith(settings.SourceBranch))
                            {
                                continue;
                            }

                            sourceFile = sourceFile.Substring(settings.SourceBranch.Length + 1);

                            dates.TryAdd((sourceFile, alias), new FileInfo(codeFile).LastWriteTime);
                        }
                    }

                    choices = choices
                        .Where(c => dates.ContainsKey((c.path, c.alias)))
                        .OrderByDescending(c => dates[(c.path, c.alias)])
                        .Take(settings.Last.Value).ToList();
                }

                if (choices.Count == 0)
                {
                    AnsiConsole.MarkupLine($"No files to reimport");
                    Environment.Exit(-1);
                }
            }
            else
            {
                var pattern = settings.SourcePattern ?? AnsiConsole.Ask("[white]Pattern to import[/]", "*.cs");

                foreach (var line in lsTreeOutput)
                {
                    var parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 4)
                        continue;

                    if (!parts[1].Equals("blob"))
                        continue;

                    if (!GitUtils.MatchPattern(pattern, parts[3]))
                        continue;

                    choices.Add((parts[3], null, parts[2]));
                }

                if (choices.Count == 0)
                {
                    AnsiConsole.MarkupLine($"No files match pattern '[teal]{pattern}[/]'");
                    Environment.Exit(-1);
                }
            }

            if (settings.All)
            {
                return choices.ToArray();
            }
            else
            {
                var filePrompt = new SelectionPrompt<(string path, string? alias, string hash)>()
                                    .Title("Select file to import")
                                    .PageSize(20)
                                    .MoreChoicesText("[grey](Move up and down to reveal more files)[/]")
                                    .UseConverter(i => $"{ShortenPath(i.path)}{(i.alias != null ? " (" + i.alias + ")" : null)} [grey]{i.hash}[/]")
                                    .AddChoices(choices);

                var file = AnsiConsole.Prompt(filePrompt);

                return [file];
            }
        }

        private static string ShortenPath(string fullPath)
        {
            // Check if the path is null or empty
            if (string.IsNullOrEmpty(fullPath))
            {
                return fullPath;
            }

            // Get the file name from the path
            string fileName = Path.GetFileName(fullPath);

            // Get the directory path without the file name
            string directoryPath = Path.GetDirectoryName(fullPath) ?? fullPath;

            if (string.IsNullOrEmpty(directoryPath))
            {
                return fullPath;
            }

            // Split the directory path into parts
            string[] pathParts = directoryPath.Split(Path.DirectorySeparatorChar);

            if (pathParts.Length < 3)
            {
                // If there's only one directory or less, return the path as is
                return fullPath;
            }

            // Combine the last directory with the file name, preceded by "..."
            string shortenedPath = string.Join("/", "...", pathParts[pathParts.Length - 2], fileName);

            return shortenedPath;
        }
    }

    class InformationExtractingSyntaxWalker : CSharpSyntaxWalker
    {
        private string _sourceFile;

        public InformationExtractingSyntaxWalker(string sourceFile)
        {
            _sourceFile = sourceFile;
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            Namespace = node.Name.ToString();

            base.VisitFileScopedNamespaceDeclaration(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            if (Namespace is null)
            {
                Namespace = node.Name.ToString();
            }
            else
            {
                Namespace += "." + node.Name.ToString();
            }

            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (RenameTypeRewriter.IsPathDependentOnTypeName(_sourceFile, node.Identifier.ToString()))
            {
                Type = node.Identifier.ToString();
            }

            base.VisitEnumDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (RenameTypeRewriter.IsPathDependentOnTypeName(_sourceFile, node.Identifier.ToString()))
            {
                Type = node.Identifier.ToString();
            }

            base.VisitClassDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (RenameTypeRewriter.IsPathDependentOnTypeName(_sourceFile, node.Identifier.ToString()))
            {
                Type = node.Identifier.ToString();
            }

            base.VisitInterfaceDeclaration(node);
        }



        public string? Namespace { get; private set; }
        public string? Type { get; private set; }
    }
}
