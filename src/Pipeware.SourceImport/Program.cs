using Pipeware.SourceImport;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
AnsiConsole.Profile.Width = int.MaxValue;

var app = new CommandApp();

app.Configure(c =>
{
    c.AddCommand<FetchCommand>("fetch");
    c.AddCommand<ImportCommand>("import");
});


return app.Run(args);