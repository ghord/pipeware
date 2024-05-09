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
    public class RemovePropertyRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Property { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Properties { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Multiple { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Property == null && Properties == null)
            {
                context.Logger.LogError("Property rewriter has neither property nor properties set");

                return tree;
            }


            var rewriter = new RemovePropertyCSharpRewriter(Properties ?? [Property!], Multiple, context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            if (!Optional)
            {
                foreach (var property in rewriter.NotRemovedProperties)
                {
                    context.Logger.LogWarning("Property {property} was not removed", property);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemovePropertyCSharpRewriter : CSharpSyntaxRewriter
        {
            private HashSet<string> _properties;
            private HashSet<string> _removedProperties = new HashSet<string>();
            private HashSet<string> _removedPropertyUsages = new HashSet<string>();
            private bool _multiple;
            private ILogger _logger;

            public RemovePropertyCSharpRewriter(string[] properties, bool multiple, ILogger logger)
            {
                _properties = properties.ToHashSet();
                _multiple = multiple;
                _logger = logger;
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (_properties.Contains(node.Identifier.ToString()))
                {
                    _logger.LogDebug("Removed property [teal]{property}[/]", node.Identifier);

                    if (!_multiple && !_removedProperties.Add(node.Identifier.ToString()))
                    {
                        _logger.LogWarning("Property {property} removed more than once", node.Identifier);
                    }

                    return null;
                }

                return base.VisitPropertyDeclaration(node);
            }
            public override SyntaxNode? VisitInitializerExpression(InitializerExpressionSyntax node)
            {
                var expressions = node.Expressions;

                var result = expressions;

                bool anyRemoved = false;

                foreach (var expression in node.Expressions)
                {
                    if (expression is AssignmentExpressionSyntax assignment && assignment.Left is IdentifierNameSyntax identifierName && _properties.Contains(identifierName.ToString()))
                    {
                        _logger.LogDebug("Removed property assignment [teal]{property}[/]", identifierName.Identifier);

                        anyRemoved = true;

                        _removedPropertyUsages.Add(identifierName.ToString());

                        result = result.Remove(expression);
                    }
                }

                if (anyRemoved)
                {
                    return Visit(node.WithExpressions(result));
                }

                return base.VisitInitializerExpression(node);
            }

            public IEnumerable<string> NotRemovedProperties => _properties.Except(_removedProperties).Except(_removedPropertyUsages);

        }
    }
}
