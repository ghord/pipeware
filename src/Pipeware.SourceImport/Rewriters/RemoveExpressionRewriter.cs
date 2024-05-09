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
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    internal class RemoveExpressionRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Expression { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Expressions { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Expression is null && Expressions is null)
            {
                context.Logger.LogError("Remove expression rewriter has neither expression nor expressions property set");

                return tree;
            }

            var root = tree.GetRoot();

            foreach (var expression in Expressions ?? [Expression!])
            {
                var rewriter = new RemoveExpressionCSharpRewriter(expression, context.Logger);

                root = rewriter.Visit(root);

                if (!Optional && !rewriter.Removed)
                {
                    context.Logger.LogWarning("Failed to remove expression '{Expression}'", Markup.Escape(expression));
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveExpressionCSharpRewriter : CSharpSyntaxRewriter
        {
            private ILogger _logger;
            private string _expression;

            public RemoveExpressionCSharpRewriter(string expression, ILogger logger)

            {
                _logger = logger;
                _expression = SyntaxFactory.ParseExpression(expression).NormalizeWhitespace().ToString();
            }

            public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
            {
                var arguments = node.Arguments;

                bool anyRemoved = false;    
                for (int i = node.Arguments.Count - 1; i >= 0; i--)
                {
                    var argument = node.Arguments[i];

                    if (argument.NormalizeWhitespace().ToString().Equals(_expression))
                    {
                        _logger.LogDebug("Removed argument '{Argument}'", Markup.Escape(argument.ToString()));

                        arguments = arguments.RemoveAt(i);
                        anyRemoved = true;
                    }
                }

                if(anyRemoved)
                {
                    Removed = true;

                    return Visit(node.WithArguments(arguments));
                }

                return base.VisitArgumentList(node);
            }

            public bool Removed { get; private set; }

        }
    }
}
