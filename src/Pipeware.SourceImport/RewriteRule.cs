using Microsoft.Extensions.FileSystemGlobbing;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Pipeware.SourceImport
{
    public class RewriteRule
    {
        private Lazy<Matcher> _matcher;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Include { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Includes { get; init; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Excludes { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Exclude { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Namespace { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Alias { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Aliases { get; init; }

        public List<IImportRewriter> Rewriters { get; set; } = new List<IImportRewriter>();


        public RewriteRule()
        {
            _matcher = new Lazy<Matcher>(BuildMatcher);
        }


        private Matcher BuildMatcher()
        {
            var result = new Matcher();

            if (Include is not null)
                result.AddInclude(Include);

            if (Includes is not null)
                result.AddIncludePatterns(Includes);

            if (Exclude is not null)
                result.AddExclude(Exclude);

            if (Excludes is not null)
                result.AddExcludePatterns(Excludes);

            return result;
        }
        public static RewriteRule CreateDefault() => new RewriteRule { Include = "**/*cs" };

        [JsonIgnore]
        public bool IsDefault => Include is not null && Include.Equals("**/*.cs") && Includes is null && Exclude is null && Excludes is null && Alias is null && Aliases is null;

        public bool MatchesPath(string path)
        {
            var absolutePath = Path.GetFullPath(path, Environment.CurrentDirectory);

            return _matcher.Value.Match(Path.GetPathRoot(Environment.CurrentDirectory)!, absolutePath).HasMatches;
        }

        public bool MatchesAlias(string? alias)
        {
            //lets say we have default alias (null), sync & async aliases
            // we want following rules to match:

            // null & sync & async - alias: null (default)
            // null & sync         - alias: !async 
            // null & async        - alias: !sync 
            // null                - aliases: [ !sync, !async ] 
            // sync                - alias: sync
            // async               - alias: async
            // sync & async        - aliases: [ sync, async ] 
            if (Aliases is not null)
            {
                bool isMatching = false;
                foreach (var str in Aliases)
                {
                    ParseAlias(str, out var match, out var negation);

                    if (negation && match.Equals(alias))
                        return false;

                    if (!negation && match.Equals(alias))
                        isMatching = true;
                }

                return isMatching;
            }
            else if (Alias is not null)
            {
                ParseAlias(Alias, out var match, out var negation);

                if (negation)
                    return !match.Equals(alias);
                else
                    return match.Equals(alias);
            }
            else
            {
                return true;
            }
        }

        private static void ParseAlias(string str, out string match, out bool isNegation)
        {
            isNegation = false;

            if (str.StartsWith('!'))
            {
                isNegation = true;
                match = str.Substring(1);
            }
            else
            {
                match = str;
            }
        }

    }
}
