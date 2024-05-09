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
    public class NamespaceRewriter : IImportRewriter
    {
        public required string SourceNamespace { get; set; }
        public required string TargetNamespace { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new NamespaceCSharpRewriter(SourceNamespace, TargetNamespace, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class NamespaceCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _sourceNamespace;
            private string _targetNamespace;
            private ILogger _logger;

            public NamespaceCSharpRewriter(string sourceNamespace, string targetNamespace, ILogger logger)
            {
                _sourceNamespace = sourceNamespace;
                _targetNamespace = targetNamespace;
                _logger = logger;
            }

            public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
            {
                if (node.NamespaceOrType.ToString().Equals(_sourceNamespace))
                {
                    if (node.Parent?.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.NamespaceOrType.ToString().Equals(_targetNamespace)) != true)
                    {
                        _logger.LogDebug("Changed using from [teal]{namespaceOrType}[/] to [green]{targetNamespace}[/]", node.NamespaceOrType, _targetNamespace);

                        return Visit(node.WithNamespaceOrType(SyntaxFactory.ParseTypeName(_targetNamespace).WithTriviaFrom(node.NamespaceOrType)));
                    }
                }
                else if (node.StaticKeyword != default)
                {
                    if (node.NamespaceOrType.ToString().StartsWith(_sourceNamespace + "."))
                    {
                        var qualifiedType = _targetNamespace + node.NamespaceOrType.ToString().Substring(_sourceNamespace.Length);

                        _logger.LogDebug("Changed static using from [teal]{namespaceOrType}[/] to [green]{qualifiedType}[/]", node.NamespaceOrType, qualifiedType);

                        return Visit(node.WithNamespaceOrType(SyntaxFactory.ParseTypeName(qualifiedType).WithTriviaFrom(node.NamespaceOrType)));
                    }
                }

                return base.VisitUsingDirective(node);
            }


        }
    }
}
