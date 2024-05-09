using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class ReplaceLiteralRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? String { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StringRegex { get; set; }

        public required JsonValue Value { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            ReplaceLiteralCSharpRewriter rewriter;

            if (String is not null)
            {
                rewriter = new ReplaceLiteralCSharpRewriter(String, false, Value.GetValue<string>(), context.Logger);
            }
            else if (StringRegex is not null)
            {
                rewriter = new ReplaceLiteralCSharpRewriter(StringRegex, true, Value.GetValue<string>(), context.Logger);
            }
            else
            {
                context.Logger.LogError("Literal rewriter has no literal source set");
                return tree;
            }

            var root = rewriter.Visit(tree.GetRoot());

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class ReplaceLiteralCSharpRewriter : CSharpSyntaxRewriter
        {
            private Func<SyntaxToken, bool> _match;
            private Func<SyntaxToken, SyntaxToken> _replacement;
            private ILogger _logger;

            public ReplaceLiteralCSharpRewriter(string source, bool regex, string target, ILogger logger)
                : this(
                      token => token.IsKind(SyntaxKind.StringLiteralToken) && 
                      (regex ? Regex.IsMatch((string)token.Value!, source) : token.Value!.Equals(source)),
                      token => regex ? SyntaxFactory.Literal(Regex.Replace((string)token.Value!, source, target)) : SyntaxFactory.Literal(target),
                      logger)
            {
            }

            private ReplaceLiteralCSharpRewriter(
                Func<SyntaxToken, bool> match,
                Func<SyntaxToken, SyntaxToken> replacement,
                ILogger logger)
            {
                _match = match;
                _replacement = replacement;
                _logger = logger;
            }

            public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                if (_match(node.Token))
                {
                    var replacement = _replacement(node.Token);

                    _logger.LogDebug("Replaced literal [teal]{expr}[/] with [green]{target}[/]", node.Token, replacement);

                    return node.WithToken(replacement.WithTriviaFrom(node.Token));
                }

                return base.VisitLiteralExpression(node);
            }
        }
    }
}
