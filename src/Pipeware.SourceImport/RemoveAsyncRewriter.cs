using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public class RemoveAsyncRewriter : IImportRewriter
    {
        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new RemoveAsyncCSharpRewriter(context.Logger);
            var root = rewriter.Visit(tree.GetRoot());

            return tree.WithRootAndOptions(root, tree.Options);
        }

      
        class RemoveAsyncCSharpRewriter : CSharpSyntaxRewriter
        {
            private ILogger _logger;

            public RemoveAsyncCSharpRewriter(ILogger logger)
            {
                _logger = logger;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if(node.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    var newModifiers = node.Modifiers.Remove(node.Modifiers.First(t => t.IsKind(SyntaxKind.AsyncKeyword)));

                    _logger.LogDebug("Removed async modifier from method {Method}.", node.Identifier);

                    return Visit(node.WithModifiers(newModifiers));
                }

                if(node.ReturnType is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals("Task"))
                {
                    var visitor = new RemoveReturnsRewriter();

                    _logger.LogDebug("Removed task return from method {Method}.", node.Identifier);

                    var result = visitor.Visit(node.WithReturnType(SyntaxFactory.ParseTypeName("void").WithTriviaFrom(node.ReturnType)));
                    return Visit(result);
                }

                return base.VisitMethodDeclaration(node);
            }

            class RemoveReturnsRewriter : CSharpSyntaxRewriter
            {
                public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
                {
                    if (node.Expression is not null)
                    {
                        return SyntaxFactory.ExpressionStatement(node.Expression).WithTriviaFrom(node);
                    }

                    return base.VisitReturnStatement(node);
                }
            }


            public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                if(node.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    var newModifiers = node.Modifiers.Remove(node.Modifiers.First(t => t.IsKind(SyntaxKind.AsyncKeyword)));

                    _logger.LogDebug("Removed async modifier from lambda expression.");

                    return node.WithModifiers(newModifiers);
                }

                return base.VisitSimpleLambdaExpression(node);
            }

            public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
            {
                if(node.Identifier.ToString().Equals("Func"))
                {
                    var funcReturn = node.TypeArgumentList.Arguments[^1];
                
                    if(funcReturn is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals("Task"))
                    {
                        var newArguments = node.TypeArgumentList.Arguments.RemoveAt(node.TypeArgumentList.Arguments.Count - 1);

                        _logger.LogDebug("Changed Func<{oldArgs}> to Action<{args}>.", node.TypeArgumentList, newArguments);

                        return Visit(node
                            .WithIdentifier(SyntaxFactory.Identifier("Action").WithTriviaFrom(node.Identifier))
                            .WithTypeArgumentList(node.TypeArgumentList.WithArguments(newArguments)));
                    }
                }

                return base.VisitGenericName(node);
            }
        }
    }
}
