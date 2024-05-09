using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class MakePartialRewriter : IImportRewriter
    {
        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new PartialCSharpRewriter(context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            if (!rewriter.MadePartial)
            {
                context.Logger.LogWarning("No type declarations made partial");
            }

            return tree.WithRootAndOptions(root ,tree.Options);
        }

        class PartialCSharpRewriter : CSharpSyntaxRewriter
        {
            bool firstDeclaration = true;
            private ILogger _logger;

            public PartialCSharpRewriter(ILogger logger)
            {
                _logger = logger;
            }

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                if (firstDeclaration)
                {
                    firstDeclaration = false;
                    _logger.LogDebug("Made interface [green]{class}[/] partial", node.Identifier);

                    return Visit(node.WithModifiers(node.Modifiers
                        .Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space))));

                }

                return base.VisitInterfaceDeclaration(node);
            }

            public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                if (firstDeclaration)
                {
                    firstDeclaration = false;
                    _logger.LogDebug("Made struct [green]{class}[/] partial", node.Identifier);

                    return Visit(node.WithModifiers(node.Modifiers
                        .Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space))));

                }


                return base.VisitStructDeclaration(node);
            }
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if(firstDeclaration)
                {
                    firstDeclaration = false;
                    _logger.LogDebug("Made class [green]{class}[/] partial", node.Identifier);

                    return Visit(node.WithModifiers(node.Modifiers
                        .Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space))));

                }
                
                return base.VisitClassDeclaration(node);
            }


            public bool MadePartial => !firstDeclaration;
            
        }
    }
}
