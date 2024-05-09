using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class ReplaceParameterRewriter : IImportRewriter
    {
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? Replacements { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Replacements is null)
            {
                context.Logger.LogError("No replacement defined in replace expression rewriter");

                return tree;
            }

            var root = tree.GetRoot();

            foreach (var (source, target) in Replacements)
            {
                if (target.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogError("Replacement target for parameter {source} is not a string", Markup.Escape(source));

                    continue;
                }

                var rewriter = new ReplaceParameterCSharpRewriter(source, target.GetString()!, context.Logger);

                root = rewriter.Visit(root);

                if (!Optional && !rewriter.Rewritten)
                {
                    context.Logger.LogWarning("Parameter {source} was not replaced", Markup.Escape(source));
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }
        class ReplaceParameterCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _source;
            private string _target;
            private ILogger _logger;

            public ReplaceParameterCSharpRewriter(string source, string target, ILogger logger)
            {
                _source = SyntaxFactory.ParseParameterList(source).Parameters[0].NormalizeWhitespace().ToString();
                _target = target;
                _logger = logger;
            }

            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (node.NormalizeWhitespace().ToString().Equals(_source.ToString()))
                {

                    _logger.LogDebug("Replaced parameter [teal]{src}[/] to [green]{target}[/]", _source, Markup.Escape(_target));

                    Rewritten = true;

                    return SyntaxFactory.ParseParameterList(_target).Parameters[0].WithTriviaFrom(node);
                }

                return base.VisitParameter(node);
            }


            public bool Rewritten { get; private set; }
        }

    }
}
