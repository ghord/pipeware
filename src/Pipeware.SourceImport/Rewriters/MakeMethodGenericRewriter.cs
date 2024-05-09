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
    public class MakeMethodGenericRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Methods { get; set; }

        public required string[] Parameters { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GenericConstraint[]? Constraints { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Arity { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Arities { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Methods == null && Method == null)
            {
                context.Logger.LogError("Make generic method rewriter has neither methods nor method property set");

                return tree;
            }

            foreach (var method in Methods ?? [Method!])
            {
                foreach (var arity in (Arities ?? [Arity]).OrderDescending())
                {
                    var rewriter = new MakeMethodGenericCSharpRewriter(method, Parameters, Constraints, arity, context.Logger);

                    tree = tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);

                    if (!Optional && !rewriter.Rewritten)
                    {
                        if (arity == 0)
                        {
                            context.Logger.LogWarning("Non-generic method {method} was not found", method);
                        }
                        else
                        {
                            context.Logger.LogWarning("Generic method {method} with arity {arity} was not found", method, arity);
                        }
                    }
                }
            }

            return tree;
        }

        class MakeMethodGenericCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _method;
            private int _arity;
            private TypeParameterListSyntax _parameters;
            private TypeArgumentListSyntax _arguments;
            private SyntaxList<TypeParameterConstraintClauseSyntax>? _constraints;
            private ILogger _logger;
            private bool _rewritten = false;
            public MakeMethodGenericCSharpRewriter(string method, string[] parameters, GenericConstraint[]? constraints, int arity, ILogger logger)
            {
                _method = method;
                _arity = arity;
                _logger = logger;

                var parameterArray = parameters.Select(p => SyntaxFactory.TypeParameter(SyntaxFactory.Identifier(p)).WithLeadingTrivia(SyntaxFactory.Whitespace(" "))).ToArray();
                var argumentArray = parameters.Select(p => SyntaxFactory.ParseTypeName(p).WithLeadingTrivia(SyntaxFactory.Whitespace(" "))).ToArray();

                if(_arity == 0)
                {
                    parameterArray[0] = parameterArray[0].WithLeadingTrivia();
                    argumentArray[0] = argumentArray[0].WithLeadingTrivia();
                }

                _parameters = SyntaxFactory.TypeParameterList().AddParameters(parameterArray);
                _arguments = SyntaxFactory.TypeArgumentList().AddArguments(argumentArray);

                if (constraints != null)
                {
                    var list = SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();

                    foreach (var constraint in constraints)
                    {
                        var typeConstraint = SyntaxFactory.TypeParameterConstraintClause(constraint.Parameter);

                        if(constraint.Class)
                        {
                            typeConstraint = typeConstraint.AddConstraints(
                                SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)).NormalizeWhitespace();
                        }

                        if (constraint.Type != null)
                        {
                            typeConstraint = typeConstraint.AddConstraints(
                                SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(constraint.Type))).NormalizeWhitespace();
                        }


                        if (list.Count == 0)
                        {
                            typeConstraint = typeConstraint.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
                        }

                        list = list.Add(typeConstraint);
                    }

                    _constraints = list;
                }
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if(_arity == 0 && node.Name is IdentifierNameSyntax identifierName && identifierName.ToString().Equals(_method))
                {
                    _logger.LogDebug("Added generic parameters to member access of [green]{method}[/]", node.Name);

                    return Visit(node.WithName(SyntaxFactory.GenericName(identifierName.Identifier, _arguments.WithLeadingTrivia())));
                }

                return base.VisitMemberAccessExpression(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (_arity == 0 && node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_method))
                {
                    _logger.LogDebug("Added generic parameters to method invocation [green]{method}[/]", identifierName.Identifier);

                    return Visit(node.WithExpression(SyntaxFactory.GenericName(identifierName.Identifier, _arguments)));
                }
                else if (_arity > 0 && node.Expression is GenericNameSyntax genericNameSyntax && genericNameSyntax.Identifier.ToString().Equals(_method) && genericNameSyntax.TypeArgumentList.Arguments.Count == _arity)
                {
                    _logger.LogDebug("Added new generic parameters to generic method invocation [green]{method}[/]", genericNameSyntax.Identifier);

                    var newArguments = genericNameSyntax.TypeArgumentList.WithArguments(genericNameSyntax.TypeArgumentList.Arguments.AddRange(_arguments.Arguments));

                    return Visit(node.WithExpression(genericNameSyntax.WithTypeArgumentList(newArguments)));
                }

                return base.VisitInvocationExpression(node);
            }

            private SyntaxList<TypeParameterConstraintClauseSyntax> WithTrailingTrivia(SyntaxList<TypeParameterConstraintClauseSyntax> value, SyntaxTriviaList trailingTrivia)
            {
                return value.Replace(value[^1], value[^1].WithTrailingTrivia(trailingTrivia));
            }


            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_method))
                {
                    if (node.TypeParameterList == null && _arity == 0)
                    {
                        _logger.LogDebug("Made method [green]{method}[/] generic", node.Identifier);
                        _rewritten = true;

                        node = node.WithTypeParameterList(_parameters);

                        if (_constraints != null)
                        {
                            node = node
                                .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.ParameterList.GetTrailingTrivia()))
                                .WithParameterList(node.ParameterList.WithTrailingTrivia());
                        }

                        return Visit(node);
                    }
                    else if (node.TypeParameterList?.Parameters.Count == _arity)
                    {
                        _logger.LogDebug("Added new generic parameters to generic method [green]{method}[/]", node.Identifier);
                        _rewritten = true;


                        node = node.WithTypeParameterList(node.TypeParameterList.WithParameters(node.TypeParameterList.Parameters.AddRange(_parameters.Parameters)));

                        if (_constraints != null)
                        {
                            if (node.ConstraintClauses.Count == 0)
                            {
                                node = node
                                    .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.ParameterList.GetTrailingTrivia()))
                                    .WithParameterList(node.ParameterList.WithTrailingTrivia());
                            }
                            else
                            {
                                node = node
                                    .WithConstraintClauses(WithTrailingTrivia(
                                        WithTrailingTrivia(node.ConstraintClauses, default)
                                            .AddRange(_constraints.Value), node.ConstraintClauses.Last().GetTrailingTrivia()));
                            }
                        }

                        return Visit(node);
                    }
                }

                return base.VisitMethodDeclaration(node);
            }


            public bool Rewritten => _rewritten;
        }
    }
}
