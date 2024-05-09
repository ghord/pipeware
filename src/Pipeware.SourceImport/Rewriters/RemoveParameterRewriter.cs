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
    public class RemoveParameterRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Parameter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Parameters { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var root = tree.GetRoot();

            foreach (var parameter in Parameters ?? [Parameter!])
            {
                var rewriter = new RemoveParameterCSharpRewriter(parameter, Method, context.Logger);

                root = rewriter.Visit(tree.GetRoot());
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveParameterCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _parameter;
            private string? _method;
            private ILogger _logger;

            public RemoveParameterCSharpRewriter(string parameter, string? method, ILogger logger)
            {
                _parameter = parameter;
                _method = method;
                _logger = logger;
            }

            public override SyntaxNode? VisitParameterList(ParameterListSyntax node)
            {
                if (node.FirstAncestorOrSelf<MemberDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
                {
                    if (_method != null && !methodDeclaration.Identifier.ToString().Equals(_method))
                    {
                        return base.VisitParameterList(node);
                    }

                    var parameters = node.Parameters;
                    bool anyChanged = false;

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        if (parameters[i].Identifier.ToString().Equals(_parameter))
                        {
                            _logger.LogDebug("Removed parameter {parameter} from method {method}", _parameter, methodDeclaration.Identifier);
                            anyChanged = true;
                            node = node.WithParameters(node.Parameters.RemoveAt(i));
                        }
                    }

                    if (anyChanged)
                        return Visit(node);
                }

                return base.VisitParameterList(node);
            }

            public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
            {
                if(node.Parent is InvocationExpressionSyntax invocationExpression)
                {
                    if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression)
                    {
                        if (_method == null || memberAccessExpression.Name.Identifier.ToString().Equals(_method))
                        {
                            var arguments = node.Arguments;
                            bool anyChanged = false;

                            for (int i = 0; i < arguments.Count; i++)
                            {
                                if (arguments[i].NameColon is { } nameColon && nameColon.Name.Identifier.ToString().Equals(_parameter))
                                {
                                    _logger.LogDebug("Removed named argument {argument} from method {method}", _parameter, memberAccessExpression.Name.Identifier);
                                    anyChanged = true;
                                    node = node.WithArguments(node.Arguments.RemoveAt(i));
                                }
                            }

                            if (anyChanged)
                                return Visit(node);
                        }
                    }
                }

                return base.VisitArgumentList(node);
            }

            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (node.FirstAncestorOrSelf<MemberDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
                {
                    if (_method != null && !methodDeclaration.Identifier.ToString().Equals(_method))
                    {
                        return base.VisitParameter(node);
                    }

                    if (node.Identifier.ToString().Equals(_parameter))
                    {
                        _logger.LogDebug("Removed parameter {parameter} from method {method}", _parameter, methodDeclaration.Identifier);

                        return null;
                    }
                }

                return base.VisitParameter(node);
            }
        }
    }
}
