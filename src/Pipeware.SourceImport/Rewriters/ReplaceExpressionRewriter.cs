using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    internal class ReplaceExpressionRewriter : IImportRewriter
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
                    context.Logger.LogError("Replacement target for {source} is not a string", Markup.Escape(source));

                    continue;
                }

                var rewriter = new ReplaceExpressionCSharpRewriter(source, target.GetString()!, context.Logger);

                root = rewriter.Visit(root);

                if (!Optional && !rewriter.Rewritten)
                {
                    context.Logger.LogWarning("Expression {source} was not replaced", Markup.Escape(source));
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }


        class ReplaceExpressionCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _source;
            private string _target;
            private ILogger _logger;

            public ReplaceExpressionCSharpRewriter(string source, string target, ILogger logger)
            {
                _source = SyntaxFactory.ParseExpression(source).NormalizeWhitespace().ToString();
                _target = target;
                _logger = logger;
            }

            [return: NotNullIfNotNull("node")]
            public override SyntaxNode? Visit(SyntaxNode? node)
            {
                node = base.Visit(node);

                if (node is ExpressionSyntax expression && expression.NormalizeWhitespace().ToString().Equals(_source))
                {
                    _logger.LogDebug("Replaced expression [teal]{src}[/] to [green]{target}[/]", Markup.Escape(_source), Markup.Escape(_target));

                    Rewritten = true;

                    return SyntaxFactory.ParseExpression(_target).WithTriviaFrom(expression);
                }

                return node;
            }

            public bool Rewritten { get; private set; }
        }
    }
}
