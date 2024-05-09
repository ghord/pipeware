using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class ReplaceCommentRewriter : IImportRewriter
    {
        [JsonIgnore(Condition =  JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Regex { get; set; }

        public required string Replacement { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if(Regex == null && Text == null)
            {
                context.Logger.LogError("Comment rewriter has neither text nor regex property set");

                return tree;
            }

            var regex = new Regex(Regex ?? System.Text.RegularExpressions.Regex.Escape(Text!));

            var rewriter = new CommentCSharpRewriter(regex, Replacement, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class CommentCSharpRewriter : CSharpSyntaxRewriter
        {
            private Regex _regex;
            private ILogger _logger;
            private string _replacement;

            public CommentCSharpRewriter(Regex regex, string replacement, ILogger logger) :
                base(visitIntoStructuredTrivia: true)
            {
                _regex = regex;
                _logger = logger;
                _replacement = replacement;
            }
            public override SyntaxNode? VisitXmlText(XmlTextSyntax node)
            {
                var input = node.ToString();
                var output = _regex.Replace(input, _replacement);

                if (!output.Equals(input, StringComparison.Ordinal))
                {
                    _logger.LogDebug("Replaced comment text [teal]{regex}[/] with [green]{replacement}[/]", Markup.Escape(_regex.ToString()), Markup.Escape(_replacement));

                    return SyntaxFactory.XmlText(output).WithTriviaFrom(node);
                }

                return base.VisitXmlText(node);
            }

        }
    }
}
