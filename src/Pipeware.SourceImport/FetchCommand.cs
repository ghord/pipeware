using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    [Description("Fetches (or clones, if it does not exist) git repository in current folder")]
    public sealed class FetchCommand : Command<FetchCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Repository to fetch. Defaults to aspnetcore sources.")]
            [CommandOption("-r|--repository")]
            [DefaultValue("https://github.com/dotnet/aspnetcore.git")]
            public required string Repository { get; init; }

            [Description("Directory in which to place source")]
            [CommandOption("-d|--dir|--directory")]
            [DefaultValue("aspnetcore")]
            public required string Directory { get; init; }

            [Description("Deletes existing directory and clones again")]
            [CommandOption("--recreate")]
            public bool Recreate { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var logger = new SpectreCliLogger(false);

            if (settings.Recreate)
            {
                if (Directory.Exists(Path.Combine(settings.Directory, ".git")))
                {
                    DeleteDirectory(settings, logger);
                }
            }

            if (!Directory.Exists(Path.Combine(settings.Directory, ".git")))
            {
                CloneDirectory(settings, logger);
            }
            else
            {
                FetchDirectory(settings, logger);
            }

            return 0;
        }

        private void FetchDirectory(Settings settings, SpectreCliLogger logger)
        {
            AnsiConsole.MarkupLineInterpolated($"Fetching '[teal]{settings.Repository}[/]' into '[teal]{settings.Directory}[/]'");

            GitUtils.SetDirectory(settings.Directory);
            var result = GitUtils.RunCommand(logger, "fetch");

            if (result.Success)
            {
                AnsiConsole.MarkupLine("[green]Success[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failure[/]");
            }
        }

        private void CloneDirectory(Settings settings, SpectreCliLogger logger)
        {
            AnsiConsole.MarkupLineInterpolated($"Cloning '[teal]{settings.Repository}[/]' into '[teal]{settings.Directory}[/]'");

            var result = GitUtils.RunCommand(logger, "clone", "--no-checkout", settings.Repository, settings.Directory);

            if(result.Success)
            {
                AnsiConsole.MarkupLine("[green]Success[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failure[/]");
            }
        }

        private void DeleteDirectory(Settings settings, SpectreCliLogger logger)
        {
            AnsiConsole.Status()
                .Start($"Deleting existing directory {settings.Directory}", ctx =>
                {
                    var directoryInfo = new DirectoryInfo(settings.Directory);

                    setAttributesNormal(directoryInfo);

                    directoryInfo.Delete(true);
                });

            void setAttributesNormal(DirectoryInfo dir)
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    setAttributesNormal(subDir);
                    subDir.Attributes = FileAttributes.Normal;
                }
                foreach (var file in dir.GetFiles())
                {
                    file.Attributes = FileAttributes.Normal;
                }
            }
        }
    }
}
