using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Threading.Tasks;
using static Pipeware.SourceImport.ImportCommand;

namespace Pipeware.SourceImport;


class ResourceFileCache
{
    private ILogger _logger;
    private ImportSettings _settings;
    private Dictionary<string, Lazy<Dictionary<string, string>>> _contents;

    public ResourceFileCache(IEnumerable<string> files, ILogger logger, ImportSettings settings)
    {
        Files = files.ToArray();
        _logger = logger;
        _settings = settings;
        _contents = files.ToDictionary(f => f, f => new Lazy<Dictionary<string, string>>(() => ParseTextResources(f), LazyThreadSafetyMode.ExecutionAndPublication));
    }

    public string[] Files { get; }

    public Dictionary<string, string> GetTextResources(string file)
    {
        return _contents[file].Value;
    }

    private Dictionary<string, string> ParseTextResources(string resourceFile)
    {
        // get resource file content
        GitUtils.SetStandardOutputCapture(true);
        GitUtils.SetDirectory(_settings.SourceDirectory);

        var gitResult = GitUtils.RunCommand(_logger, "show", "origin/" + _settings.SourceBranch + ":" + resourceFile);
        var xmlContent = string.Join(Environment.NewLine, gitResult.StandardOutput!);

        xmlContent = xmlContent.Trim([ '\uFEFF', '\u200B' ]);

        var result = new Dictionary<string, string>();
        using (var reader = new ResXResourceReader(new StringReader(xmlContent)))
        {
            foreach (DictionaryEntry entry in reader)
            {
                if (entry.Value is not null)
                {
                    result.Add(entry.Key.ToString()!, entry.Value.ToString()!);
                }
            }
        }

        return result;
    }

}
