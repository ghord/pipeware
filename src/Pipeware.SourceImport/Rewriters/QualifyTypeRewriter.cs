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
    public class QualifyTypeRewriter : IImportRewriter
    {
        public required string Namespace { get; set; }
        public required string Type { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new QualifyTypeCSharpRewriter(Type, Namespace, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class QualifyTypeCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _identifier;
            private string _namespace;
            private ILogger _logger;

            public QualifyTypeCSharpRewriter(string identifier, string @namespace, ILogger logger)
            {
                _identifier = identifier;
                _namespace = @namespace;
                _logger = logger;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                return base.VisitMethodDeclaration(node);
            }
            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.ToString().Equals(_identifier))
                {
                    _logger.LogDebug("Qualified type [teal]{type}[/] with namespace [green]{namespace}[/]", node.Identifier, _namespace);

                    return node.WithIdentifier(SyntaxFactory.Identifier(_namespace + "." + _identifier));
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}
