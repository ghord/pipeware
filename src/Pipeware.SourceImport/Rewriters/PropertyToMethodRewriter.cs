using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class PropertyToMethodRewriter : IImportRewriter
    {
        public required string Property { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new PropertyToMethodCSharpRewriter(Property, Method ?? Property, context.Logger);

            var result = rewriter.Visit(tree.GetRoot());

            return tree.WithRootAndOptions(result, tree.Options);
        }

        class PropertyToMethodCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _property;
            private string _method;
            private ILogger _logger;

            public PropertyToMethodCSharpRewriter(string property, string method, ILogger logger)
            {
                _property = property;
                _method = method;
                _logger = logger;
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node.Name is SimpleNameSyntax simpleName && simpleName.Identifier.ToString().Equals(_property))
                {
                    _logger.LogDebug("Rewritten property access [teal]{property}[/] to method [green]{method}[/]", simpleName.Identifier, _method);

                    if(_method != _property)
                    {
                        node = node.WithName(simpleName.WithIdentifier(SyntaxFactory.Identifier(_method)));
                    }

                    return Visit(SyntaxFactory.InvocationExpression(node, SyntaxFactory.ArgumentList()));
                }

                return base.VisitMemberAccessExpression(node);
            }
        }
    }
}
