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
    public class NamespaceDeclarationRewriter : IImportRewriter
    {
        public required string TargetNamespace { get; init; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new NamespaceSyntaxRewriter(TargetNamespace, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }


        class NamespaceSyntaxRewriter : CSharpSyntaxRewriter
        {
            private string _targetNamespace;
            private ILogger _logger;

            public NamespaceSyntaxRewriter(string targetNamespace, ILogger logger)
            {
                _targetNamespace = targetNamespace;
                _logger = logger;
            }

            public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                throw new NotImplementedException();
            }

            public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                _logger.LogDebug("Changed namespace from [teal]{sourceNamespace}[/] to [green]{targetNamespace}[/]", node.Name, _targetNamespace);

                return node.WithName(SyntaxFactory.ParseName(_targetNamespace));
            }
        }
    }
}
