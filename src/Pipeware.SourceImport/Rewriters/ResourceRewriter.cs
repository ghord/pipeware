using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class ResourceRewriter : IImportRewriter
    {
        private Dictionary<string, string> _resources;

        public ResourceRewriter(Dictionary<string, string> resources)
        {
            _resources = resources;
        }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new ResourceCSharpRewriter(_resources, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class ResourceCSharpRewriter : CSharpSyntaxRewriter
        {
            private Dictionary<string, string> _resources;
            private ILogger _logger;

            public ResourceCSharpRewriter(Dictionary<string, string> resources, ILogger logger)
            {
                _resources = resources;
                _logger = logger;
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node.Expression.ToString().Equals("Resources"))
                {
                    var resourceKey = node.Name.ToString();

                    if (_resources.TryGetValue(resourceKey, out var resourceString))
                    {
                        _logger.LogDebug("Rewritten resource usage of [teal]{resourceKey}[/] property at call site", resourceKey);

                        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(resourceString));
                    }
                }

                return base.VisitMemberAccessExpression(node);
            }

            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (node.Type is not SimpleNameSyntax simpleName || simpleName.Identifier.Equals("LocalizableResourceString"))
                    goto notLocalizable;

                if (node.ArgumentList?.Arguments.Count != 3)
                    goto notLocalizable;

                if (node.ArgumentList.Arguments[0].Expression is not InvocationExpressionSyntax nameOfExpression)
                    goto notLocalizable;

                if (nameOfExpression.Expression is not IdentifierNameSyntax nameofIdentifier || nameofIdentifier.Identifier.Equals("nameof"))
                    goto notLocalizable;

                if (nameOfExpression.ArgumentList.Arguments.Count != 1)
                    goto notLocalizable;

                if (nameOfExpression.ArgumentList.Arguments[0].Expression is not MemberAccessExpressionSyntax memberAccessExpression)
                    goto notLocalizable;

                if(memberAccessExpression.Expression.ToString().Equals("Resources"))
                {
                    var resourceKey = memberAccessExpression.Name.ToString();

                    if (_resources.TryGetValue(resourceKey, out var resourceString))
                    {
                        _logger.LogDebug("Rewritten resource usage of [teal]{resourceKey}[/] property for LocalizableResourceString", resourceKey);

                        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(resourceString)).WithTriviaFrom(node);
                    }
                }

            notLocalizable:
                return base.VisitObjectCreationExpression(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is MemberAccessExpressionSyntax memberAccessExpression)
                {
                    var resourceKey = memberAccessExpression.Name.ToString();

                    if (resourceKey.StartsWith("Format"))
                    {
                        if (_resources.TryGetValue(resourceKey.Substring(6), out var resourceString))
                        {
                            var literal = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(resourceString));

                            _logger.LogDebug("Rewritten resource usage of [teal]{resourceKey}()[/] method at call site", resourceKey);

                            return Visit(SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.ParseTypeName("string"),
                                    SyntaxFactory.IdentifierName("Format")),
                                node.ArgumentList.WithArguments(
                                     node.ArgumentList.Arguments.Insert(0, SyntaxFactory.Argument(literal))
                                ).NormalizeWhitespace()
                                ));
                        }
                    }
                }

                return base.VisitInvocationExpression(node);
            }
        }
    }
}
