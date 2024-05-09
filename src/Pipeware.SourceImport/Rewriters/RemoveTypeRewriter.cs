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
    public class RemoveTypeRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Types { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Type == null && Types == null)
            {
                context.Logger.LogError("Property rewriter has neither type nor types set");

                return tree;
            }


            var rewriter = new RemoteTypeCSharpRewriter(Types ?? [Type!], context.Logger);

            return tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
        }

        class RemoteTypeCSharpRewriter : CSharpSyntaxRewriter
        {
            private HashSet<string> _types;
            private ILogger _logger;

            public RemoteTypeCSharpRewriter(string[] types, ILogger logger)
            {
                _types = types.ToHashSet();
                _logger = logger;
            }

            public override SyntaxNode? VisitBaseList(BaseListSyntax node)
            {
                var typesToRemove = node.Types.Where(t => t is SimpleBaseTypeSyntax simpleType && _types.Contains(simpleType.Type.ToString())).ToArray();

                if (typesToRemove.Length > 0)
                {
                    foreach (var type in typesToRemove)
                    {
                        _logger.LogDebug("Removed base type [teal]{type}[/]", type.Type);
                    }

                    var finalTypes = typesToRemove.Aggregate(node.Types, (types, typeToRemove) => types.Remove(typeToRemove));

                    if (typesToRemove.Last() == node.Types.Last())
                    {
                        if (finalTypes.Count > 0)
                        {
                            finalTypes = finalTypes.Replace(finalTypes.Last(), finalTypes.Last().WithTrailingTrivia(typesToRemove.Last().GetTrailingTrivia()));
                        }
                    }

                    return Visit(node.WithTypes(finalTypes));
                }

                return base.VisitBaseList(node);
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.ExplicitInterfaceSpecifier != null && _types.Contains(node.ExplicitInterfaceSpecifier.Name.ToString()))
                {
                    _logger.LogDebug("Removed explicit interface [teal]{interface}[/] implementing method [teal]{methodName}[/]", node.ExplicitInterfaceSpecifier.Name, node.Identifier);

                    return null;
                }

                return base.VisitMethodDeclaration(node);
            }
        }
    }
}
