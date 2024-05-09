using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RemoveGenericParameterRewriter : IImportRewriter
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (ExtensionData is null)
            {
                context.Logger.LogWarning("No parameters were provided to the remove generic parameter rewriter.");

                return tree;
            }

            var root = tree.GetRoot();

            foreach (var (parameter, replacementJson) in ExtensionData)
            {
                if (replacementJson.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogWarning("Invalid replacement value for parameter {Parameter}. Expected string, but got {ValueKind}.", parameter, replacementJson.ValueKind);

                    continue;
                }

                var rewriter = new RemoveGenericParameterCSharpRewriter(parameter, replacementJson.GetString()!, context.Logger);

                root = rewriter.Visit(root);

                if (!rewriter.Removed && !Optional)
                {
                    context.Logger.LogWarning("No generic parameter {Parameter} was found to remove.", parameter);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveGenericParameterCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _parameter;
            private string _replacement;
            private ILogger _logger;

            public bool Removed { get; private set; }

            public RemoveGenericParameterCSharpRewriter(string parameter, string replacement, ILogger logger)
            {
                _parameter = parameter;
                _replacement = replacement;
                _logger = logger;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.TypeParameterList != null)
                {
                    var parameter = node.TypeParameterList.Parameters.FirstOrDefault(p => p.Identifier.ToString().Equals(_parameter));

                    if (parameter is not null)
                    {
                        if (node.Arity == 1)
                        {
                            _logger.LogDebug("Removed generic parameter {Parameter} from method {Method}, making it non-generic", _parameter, node.Identifier);

                            Removed = true;

                            return Visit(node.WithTypeParameterList(null));
                        }
                        else
                        {
                            _logger.LogDebug("Removed generic parameter {Parameter} from generic method {Method}", _parameter, node.Identifier);

                            Removed = true;

                            return Visit(node.WithTypeParameterList(node.TypeParameterList.WithParameters(node.TypeParameterList.Parameters.Remove(parameter))));
                        }
                    }
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if(node.Identifier.ToString().Equals(_parameter))
                {
                    _logger.LogDebug("Replaced type parameter usage {Parameter} in {Node}", _parameter, node.Parent);

                    return SyntaxFactory.ParseTypeName(_replacement).WithTriviaFrom(node);
                }

                return base.VisitIdentifierName(node);
            }

            public override SyntaxNode? VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
            {
                if (node.Name.Identifier.ToString().Equals(_parameter))
                {
                    _logger.LogDebug("Removed constraint [teal]{Clause}[/] for type parameter {Parameter}", node, _parameter);

                    return null;
                }

                return base.VisitTypeParameterConstraintClause(node);
            }

        }
    }
}
