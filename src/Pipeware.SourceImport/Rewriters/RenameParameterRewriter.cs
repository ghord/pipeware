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
    public class RenameParameterRewriter : IImportRewriter
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (ExtensionData is null || ExtensionData.Count == 0)
            {
                context.Logger.LogWarning("No parameters to rename.");
                return tree;
            }

            var root = tree.GetRoot();

            foreach (var (sourceName, targetNameJson) in ExtensionData)
            {
                if (targetNameJson.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogWarning("Target name for parameter {SourceName} is not a string.", sourceName);
                    continue;
                }

                var targetName = targetNameJson.GetString()!;

                var rewriter = new RenameParameterCSharpRewriter(sourceName, targetName, context.Logger);

                root = rewriter.Visit(root);

                if(!Optional && !rewriter.Renamed)
                {
                    context.Logger.LogWarning("Parameter {SourceName} was not renamed.", sourceName);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RenameParameterCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _sourceName;
            private string _targetName;
            private ILogger _logger;

            public bool Renamed { get; private set; }

            public RenameParameterCSharpRewriter(string sourceName, string targetName, ILogger logger)
            {
                _sourceName = sourceName;
                _targetName = targetName;
                _logger = logger;
            }

            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Renamed parameter {SourceName} to {TargetName}", _sourceName, _targetName);

                    Renamed = true;

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName)));
                }

                return base.VisitParameter(node);
            }

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced parameter in argument [teal]{argument}[/] with name [green]{target}[/]", node, _targetName);

                    return Visit(node.WithExpression(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));
                }


                return base.VisitArgument(node);
            }
            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if(node.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced identifier [teal]{argument}[/] with name [green]{target}[/]", node, _targetName);

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(node.Identifier)));
                }

                return base.VisitIdentifierName(node);
            }
          

        }

    }
}
