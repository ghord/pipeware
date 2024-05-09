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
    public class RenamePropertyRewriter : IImportRewriter
    {

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Renames { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; } = false;

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Renames == null || Renames.Count == 0)
            {
                context.Logger.LogError("Missing type renames in property rewriter");

                return tree;
            }

            var root = tree.GetRoot();

            foreach (var (sourceName, targetNameJson) in Renames)
            {
                if (targetNameJson.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogWarning("Target name for {sourceName} is not a string in type rewriter", sourceName);

                    continue;
                }

                var targetName = targetNameJson.GetString()!;

                var rewriter = new RenamePropertyCSharpRewriter(sourceName, targetName, context.Logger);

                root = rewriter.Visit(root);

                if (!Optional && !rewriter.Renamed)
                {
                    context.Logger.LogWarning("Property {SourceName} was not renamed", sourceName);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RenamePropertyCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _sourceName;
            private string _targetName;
            private ILogger _logger;

            public bool Renamed { get; private set; }

            public RenamePropertyCSharpRewriter(string sourceName, string targetName, ILogger logger) : base(true)
            {
                _sourceName = sourceName;
                _targetName = targetName;
                _logger = logger;
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced property declaration [teal]{property}[/] with name [green]{target}[/]", node.Identifier, _targetName);

                    Renamed = true;

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(node.Identifier)));
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.Left is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced property assignment [teal]{property}[/] with name [green]{target}[/]", identifierName.Identifier, _targetName);

                    return Visit(node.WithLeft(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));
                }

                return base.VisitAssignmentExpression(node);
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node.Name.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced property access [teal]{property}[/] with name [green]{target}[/]", node, _targetName);

                    return Visit(node.WithName((SimpleNameSyntax)SyntaxFactory.ParseName(_targetName).WithTriviaFrom(node.Name)));
                }

                return base.VisitMemberAccessExpression(node);
            }


            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced property in argument [teal]{property}[/] with name [green]{target}[/]", node, _targetName);

                    return Visit(node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));

                }


                return base.VisitArgument(node);
            }

            public override SyntaxNode? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced identifier [teal]{property}[/] with name [green]{target}[/]", node, _targetName);

                    return Visit(node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));
                }

                return base.VisitArrowExpressionClause(node);
            }
        }
    }
}
