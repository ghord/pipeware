using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Pipeware.SourceImport.Rewriters
{
    public class RemoveMethodRewriter : MethodRewriterBase
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Keep { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Multiple { get; set; }

        public override SyntaxTree Rewrite(RewriterContext context, MethodConstraint methodConstraint, SyntaxTree tree)
        {
        
            var rewriter = new RemoveMethodCSharpRewriter(
                methodConstraint,
                Multiple,
                Keep,
                context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            foreach (var method in rewriter.UnprocessedMethods)
            {
                if (Keep)
                {
                    context.Logger.LogWarning("Method {method} was not found", method);
                }
                else
                {
                    context.Logger.LogWarning("Method {method} was not removed", method);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveMethodCSharpRewriter : CSharpSyntaxRewriter
        {
            private bool _multiple;
            private MethodConstraint _constraint;
            private ILogger _logger;
            private bool _keep;
            private HashSet<string> _unprocessedMethods;

            public RemoveMethodCSharpRewriter(MethodConstraint methodConstraint, bool multiple, bool keep, ILogger logger)
            {
                _multiple = multiple;
                _constraint = methodConstraint;
                _unprocessedMethods = keep ? methodConstraint.Methods!.ToHashSet() : [];
                _logger = logger;
                _keep = keep;
            }

            public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            {
                if (!_constraint.MatchesDeclaringType(node))
                    return base.VisitLocalFunctionStatement(node);

                var methodMatch =
                    _constraint.MatchesArity(node.TypeParameterList) &&
                    _constraint.MatchesParameters(node.ParameterList) &&
                    _constraint.MatchesMethodName(node.Identifier);

                if (methodMatch ^ _keep)
                {
                    if (_keep)
                    {
                        _logger.LogDebug("Removed method [teal]{methodName}{params}[/]", node.Identifier, node.ParameterList);
                        _unprocessedMethods.Remove(node.Identifier.ToString());
                    }
                    else
                    {
                        _logger.LogDebug("Removed local function [teal]{functionName}{params}[/]", node.Identifier, node.ParameterList);

                        if (!_unprocessedMethods.Add(node.Identifier.ToString()) && !_multiple)
                        {
                            _logger.LogWarning("Local function {functionName} removed more than once[/]", node.Identifier);
                        }

                        return null;
                    }
                }

                return base.VisitLocalFunctionStatement(node);
            }


            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (!_constraint.MatchesDeclaringType(node))
                    return base.VisitMethodDeclaration(node);

                var methodMatch =
                    _constraint.MatchesArity(node.TypeParameterList) &&
                    _constraint.MatchesParameters(node.ParameterList) &&
                    _constraint.MatchesMethodName(node.Identifier);

                if (methodMatch ^ _keep)
                {
                    if (_keep)
                    {
                        _logger.LogDebug("Removed method [teal]{methodName}{params}[/]", node.Identifier, node.ParameterList);
                        _unprocessedMethods.Remove(node.Identifier.ToString());

                        return null;
                    }
                    else
                    {
                        _logger.LogDebug("Removed method [teal]{methodName}{params}[/]", node.Identifier, node.ParameterList);

                        if (!_unprocessedMethods.Add(node.Identifier.ToString()) && !_multiple)
                        {
                            _logger.LogWarning("Method {method} removed more than once", node.Identifier);
                        }

                        return null;
                    }
                }

                return base.VisitMethodDeclaration(node);
            }

           

            public IEnumerable<string> UnprocessedMethods => _constraint.Methods.Except(_unprocessedMethods);
        }

    }
}
