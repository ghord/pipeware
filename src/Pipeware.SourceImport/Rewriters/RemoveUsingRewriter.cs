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
    public class RemoveUsingRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Pattern { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Using { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UsingAlias { get; set; }
        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Pattern == null && Using == null && UsingAlias == null)
            {
                context.Logger.LogError("Pattern, Using or UsingAlias must be set for remove using rewriter");

                return tree;
            }

            var rewriter = new RemoveUsingSyntaxRewriter(Using, UsingAlias, Pattern, context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class RemoveUsingSyntaxRewriter : CSharpSyntaxRewriter
        {
            private string? _pattern;
            private string? _alias;
            private string? _namespaceOrType;
            private ILogger _logger;

            public RemoveUsingSyntaxRewriter(string? namespaceOrType, string? alias, string? pattern, ILogger logger)
            {
                _pattern = pattern;
                _namespaceOrType = namespaceOrType;
                _alias = alias;
                _logger = logger;
            }

            public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
            {
                var usingsToRemove = GetUsingsToRemove(node.Usings);

                if (usingsToRemove.Count > 0)
                {
                    node = node.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepExteriorTrivia)!;
                }

                return base.VisitCompilationUnit(node);
            }

            public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                var usingsToRemove = GetUsingsToRemove(node.Usings);

                if (usingsToRemove.Count > 0)
                {
                    node = node.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepExteriorTrivia)!;
                }

                return base.VisitFileScopedNamespaceDeclaration(node);
            }

            public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                var usingsToRemove = GetUsingsToRemove(node.Usings);

                if (usingsToRemove.Count > 0)
                {
                    node = node.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepExteriorTrivia)!;
                }

                return base.VisitNamespaceDeclaration(node);
            }

            private List<UsingDirectiveSyntax> GetUsingsToRemove(SyntaxList<UsingDirectiveSyntax> usings)
            {
                var usingsToRemove = new List<UsingDirectiveSyntax>();

                foreach (var usingDirective in usings)
                {
                    if (_pattern is not null)
                    {
                        if (GitUtils.MatchPattern(_pattern, usingDirective.NamespaceOrType.ToString()))
                        {
                            _logger.LogDebug("Removed using [teal]{namespaceOrType}[/] via pattern [green]{pattern}[/]", usingDirective.NamespaceOrType, Markup.Escape(_pattern));

                            usingsToRemove.Add(usingDirective);
                        }
                    }
                    else if (_namespaceOrType is not null && usingDirective.NamespaceOrType.ToString().Equals(_namespaceOrType))
                    {
                        _logger.LogDebug("Removed using [teal]{namespaceOrType}[/]", usingDirective.NamespaceOrType);

                        usingsToRemove.Add(usingDirective);
                    }
                    else if (_alias is not null && usingDirective.Alias != null && usingDirective.Alias.Name.Identifier.ToString().Equals(_alias))
                    {
                        _logger.LogDebug("Removed using alias [teal]{alias}[/]", usingDirective.Alias.Name);

                        usingsToRemove.Add(usingDirective);
                    }
                }

                return usingsToRemove;
            }
        }
    }
}
