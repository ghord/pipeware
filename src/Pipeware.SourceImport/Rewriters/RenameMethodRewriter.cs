using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RenameMethodRewriter : IImportRewriter
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Renames { get; set; } = new Dictionary<string, JsonElement>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]

        public bool Multiple { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if(Renames is null || Renames.Count == 0)
            {
                context.Logger.LogError("No methods to rename");

                return tree;
            }

            var root = tree.GetRoot();

            foreach(var (source, targetJson) in Renames)
            {
                if(targetJson.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogError("Invalid target name for method {method}", source);
                    continue;
                }

                var target = targetJson.GetString()!;

                var rewriter = new RenameMethodCSharpRewriter(source, target, Multiple, context.Logger);

                root = rewriter.Visit(root);

                if(!rewriter.Renamed)
                {
                    context.Logger.LogWarning($"Method {source} was not renamed");
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RenameMethodCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _source;
            private string _target;
            private ILogger _logger;
            private bool _multiple;

            public bool Renamed { get; private set; }

            public RenameMethodCSharpRewriter(string source, string target, bool multiple, ILogger logger)
            {
                _source = source;
                _target = target;
                _logger = logger;
                _multiple = multiple;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_source))
                {
                    if (!_multiple && Renamed)
                    {
                        _logger.LogWarning("Method {method} renamed more than once", node.Identifier);
                    }

                    _logger.LogDebug("Renamed method [teal]{methodName}{params)}[/] to [green]{target}[/]", node.Identifier, node.ParameterList, _target);

                    Renamed = true;

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_target).WithTriviaFrom(node.Identifier)));
                }


                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_source))
                {
                    _logger.LogDebug("Renamed method usage [teal]{methodName}[/] to [green]{target}[/]", identifierName.Identifier, _target);

                    return Visit(node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(_target).WithTriviaFrom(identifierName.Identifier))));
                }
                else if (node.Expression is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_source))
                {
                    _logger.LogDebug("Renamed method usage [teal]{methodName}[/] to [green]{target}[/]", genericName.Identifier, _target);

                    return Visit(node.WithExpression(genericName.WithIdentifier(SyntaxFactory.Identifier(_target).WithTriviaFrom(genericName.Identifier))));
                }

                return base.VisitInvocationExpression(node);
            }
        }
    }
}
