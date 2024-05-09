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

    public class MakeTypeGenericRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Types { get; set; }
        public required string[] Parameters { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GenericConstraint[]? Constraints { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Arity { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Arities { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Types == null && Type == null)
            {
                context.Logger.LogError("Make generic type rewriter has neither types nor type property set.");

                return tree;
            }

            foreach (var arity in (Arities ?? [Arity]).OrderDescending())
            {
                var rewriter = new MakeTypeGenericCSharpRewriter(Types ?? [Type!], Parameters, Constraints, arity, context.Logger);

                tree = tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
            }

            return tree;
        }

        class MakeTypeGenericCSharpRewriter : CSharpSyntaxRewriter
        {
            private int _arity;
            private HashSet<string> _types;
            private TypeParameterListSyntax _parameters;
            private TypeArgumentListSyntax _arguments;
            private ILogger _logger;

            private SyntaxList<TypeParameterConstraintClauseSyntax>? _constraints;
            public MakeTypeGenericCSharpRewriter(string[] types, string[] parameters, GenericConstraint[]? constraints, int arity, ILogger logger)
                : base(true)
            {
                _arity = arity;
                _logger = logger;
                _types = types.ToHashSet();

                var parameterArray = parameters.Select(p => SyntaxFactory.TypeParameter(SyntaxFactory.Identifier(p))).ToArray();
                var argumentsArray = parameters.Select(p => SyntaxFactory.ParseTypeName(p)).ToArray();

                if (_arity > 0)
                {
                    parameterArray[0] = parameterArray[0].WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
                    argumentsArray[0] = argumentsArray[0].WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
                }

                _parameters = SyntaxFactory.TypeParameterList().AddParameters(parameterArray);
                _arguments = SyntaxFactory.TypeArgumentList().AddArguments(argumentsArray);

                if (constraints != null)
                {
                    var list = SyntaxFactory.List<TypeParameterConstraintClauseSyntax>();

                    foreach (var constraint in constraints)
                    {
                        var typeConstraint = SyntaxFactory.TypeParameterConstraintClause(constraint.Parameter);

                        if (constraint.Class)
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


         
            
            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made type constructor [green]{identifier}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made type constructor [green]{identifier}[/] generic", genericName.Identifier);

                    return Visit(node.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(
                                _arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.Identifier.TrailingTrivia))));
                }

                return base.VisitObjectCreationExpression(node);
            }


            public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made cast expression [green]{nodeType}[/] generic", node.Type);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made cast expression [green]{nodeType}[/] generic", node.Type);

                    return Visit(node.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(
                                _arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitCastExpression(node);
            }


            public override SyntaxNode? VisitNullableType(NullableTypeSyntax node)
            {
                if (_arity == 0 && node.ElementType is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made nullable type [green]{identifier}[/] generic", identifierName.Identifier);

                    return Visit(node.WithElementType(SyntaxFactory.GenericName(identifierName.Identifier, _arguments)));
                }
                else if (_arity > 0 && node.ElementType is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made nullable generic type [green]{identifier}[/] generic", genericName.Identifier);

                    return Visit(node.WithElementType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitNullableType(node);
            }

            public override SyntaxNode? VisitRefType(RefTypeSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made ref type [green]{identifier}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(), _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }

                return base.VisitRefType(node);
            }


            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierNameSyntax && _types.Contains(identifierNameSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Made parameter [green]{node}[/] generic", node.ToString());

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierNameSyntax.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierNameSyntax.Identifier.TrailingTrivia))));
                }

                return base.VisitParameter(node);
            }
            public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
            {
                var result = node.Arguments;
                bool anyReplaced = false;

                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    if (_arity == 0 && node.Arguments[i] is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        result = result.Replace(result[i], SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia)));

                        anyReplaced = true;
                    }
                    else if (_arity > 0 && node.Arguments[i] is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                    {
                        result = result.Replace(result[i], genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia())));

                        anyReplaced = true;
                    }
                }

                if (anyReplaced)
                {
                    _logger.LogDebug("Made type arguments [teal]{args}[/] generic", node.Arguments.ToString());

                    return Visit(node.WithArguments(result));
                }

                return base.VisitTypeArgumentList(node);
            }


            public override SyntaxNode? VisitTupleType(TupleTypeSyntax node)
            {
                var result = node.Elements;
                bool anyReplaced = false;

                for (int i = 0; i < node.Elements.Count; i++)
                {
                    if (_arity == 0 && node.Elements[i].Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        result = result.Replace(result[i], node.Elements[i].WithType(SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));

                        anyReplaced = true;
                    }
                    else if (_arity > 0 && node.Elements[i].Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                    {
                        result = result.Replace(result[i], node.Elements[i].WithType(genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));

                        anyReplaced = true;
                    }
                }

                if (anyReplaced)
                {
                    _logger.LogDebug("Made tuple type arguments [teal]{args}[/] generic", node.Elements.ToString());

                    return Visit(node.WithElements(result));
                }

                return base.VisitTupleType(node);
            }

            private SyntaxList<TypeParameterConstraintClauseSyntax> WithTrailingTrivia(SyntaxList<TypeParameterConstraintClauseSyntax> value, SyntaxTriviaList trailingTrivia)
            {
                return value.Replace(value[^1], value[^1].WithTrailingTrivia(trailingTrivia));
            }

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                if (_arity == 0 && node.TypeParameterList == null && _types.Contains(node.Identifier.ToString()) && node.ParameterList == null)
                {
                    _logger.LogDebug("Made interface [green]{identifier}[/] generic", node.Identifier.ToString());

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.BaseList != null ? SyntaxFactory.Whitespace(" ") : default))
                            .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia())
                            .WithBaseList(node.BaseList?.WithoutTrailingTrivia());
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }

                    return Visit(node);
                }
                else if (_arity > 0 && node.Arity == _arity && _types.Contains(node.Identifier.ToString()))
                {
                    _logger.LogDebug("Made interface [green]{interface}[/] generic", node.Identifier.ToString());

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray())
                                .WithTrailingTrivia(node.BaseList != null || node.ConstraintClauses.Count > 0 ? SyntaxFactory.Whitespace(" ") : default))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia())
                            .WithBaseList(node.BaseList?.WithoutTrailingTrivia());

                        if (node.ConstraintClauses.Count == 0)
                        {
                            node = node.WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia));
                        }
                        else
                        {
                            node = node.WithConstraintClauses(WithTrailingTrivia(node.ConstraintClauses.AddRange(_constraints.Value), node.ConstraintClauses[^1].GetTrailingTrivia()));
                        }
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray()).WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }

                    return Visit(node);
                }

                return base.VisitInterfaceDeclaration(node);
            }

            public override SyntaxNode? VisitDeclarationPattern(DeclarationPatternSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made declaration pattern [green]{declaration}[/] generic", node.ToString());

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }

                return base.VisitDeclarationPattern(node);
            }
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (_arity == 0 && node.TypeParameterList == null && _types.Contains(node.Identifier.ToString()) && node.ParameterList == null)
                {
                    _logger.LogDebug("Made class [green]{class}[/] generic", node.Identifier);

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.BaseList != null ? SyntaxFactory.Whitespace(" ") : default))
                            .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia())
                            .WithBaseList(node.BaseList?.WithoutTrailingTrivia());
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());

                    }

                    return Visit(node);
                }
                else if (_arity > 0 && node.Arity == _arity && _types.Contains(node.Identifier.ToString()))
                {
                    _logger.LogDebug("Made class [green]{class}[/] generic", node.Identifier);

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray())
                                .WithTrailingTrivia(node.BaseList != null || node.ConstraintClauses.Count > 0 ? SyntaxFactory.Whitespace(" ") : default))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());

                        if (node.ConstraintClauses.Count == 0)
                        {
                            node = node
                                .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia))
                                .WithBaseList(node.BaseList?.WithoutTrailingTrivia());
                        }
                        else
                        {
                            node = node.WithConstraintClauses(WithTrailingTrivia(node.ConstraintClauses.AddRange(_constraints.Value), node.ConstraintClauses[^1].GetTrailingTrivia()));
                        }
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray()).WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }

                    return Visit(node);
                }

                return base.VisitClassDeclaration(node);
            }


            public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                if (_arity == 0 && node.TypeParameterList == null && _types.Contains(node.Identifier.ToString()) && node.ParameterList == null)
                {
                    _logger.LogDebug("Made struct [green]{class}[/] generic", node.Identifier);

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.BaseList != null ? SyntaxFactory.Whitespace(" ") : default))
                            .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia())
                            .WithBaseList(node.BaseList?.WithoutTrailingTrivia());
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(_parameters.WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());

                    }

                    return Visit(node);
                }
                else if (_arity > 0 && node.Arity == _arity && _types.Contains(node.Identifier.ToString()))
                {
                    _logger.LogDebug("Made struct [green]{class}[/] generic", node.Identifier);

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray())
                                .WithTrailingTrivia(node.BaseList != null || node.ConstraintClauses.Count > 0 ? SyntaxFactory.Whitespace(" ") : default))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());

                        if (node.ConstraintClauses.Count == 0)
                        {
                            node = node
                                .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.BaseList?.GetTrailingTrivia() ?? node.Identifier.TrailingTrivia))
                                .WithBaseList(node.BaseList?.WithoutTrailingTrivia());
                        }
                        else
                        {
                            node = node.WithConstraintClauses(WithTrailingTrivia(node.ConstraintClauses.AddRange(_constraints.Value), node.ConstraintClauses[^1].GetTrailingTrivia()));
                        }
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(node.TypeParameterList!.AddParameters(_parameters.Parameters.ToArray()).WithTrailingTrivia(node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }

                    return Visit(node);
                }

                return base.VisitStructDeclaration(node);
            }

            public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
            {
                if (_arity == 0 && _types.Contains(node.Identifier.ToString()) && node.TypeParameterList == null)
                {
                    _logger.LogDebug("Made delegate [green]{delegate}[/] generic", node.Identifier);

                    if (_constraints != null)
                    {
                        node = node
                            .WithTypeParameterList(_parameters)
                            .WithConstraintClauses(WithTrailingTrivia(_constraints.Value, node.Identifier.TrailingTrivia))
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }
                    else
                    {
                        node = node
                            .WithTypeParameterList(_parameters)
                            .WithIdentifier(node.Identifier.WithTrailingTrivia());
                    }
                }

                return base.VisitDelegateDeclaration(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax nameofIdentifier && nameofIdentifier.Identifier.ToString().Equals("nameof"))
                {
                    if (_arity == 0 && node.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        _logger.LogDebug("Made type [teal]{type}[/] generic in nameof expression", identifierName.ToString());

                        return node.WithArgumentList(
                                    node.ArgumentList.WithArguments(
                                        node.ArgumentList.Arguments.Replace(
                                            node.ArgumentList.Arguments[0],
                                            node.ArgumentList.Arguments[0].WithExpression(
                                                SyntaxFactory.GenericName(identifierName.Identifier, _arguments).WithTrailingTrivia(identifierName.Identifier.TrailingTrivia)
                                            ))));
                    }
                }

                return base.VisitInvocationExpression(node);
            }





            public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.Kind() == SyntaxKind.AsExpression)
                {
                    if (_arity == 0 && node.Right is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        _logger.LogDebug("Replaced as expression type [teal]{identifierName}[/] with generic type", identifierName);

                        return Visit(node.WithRight(SyntaxFactory.GenericName(identifierName.Identifier, _arguments)));
                    }
                }
                else if(node.Kind() == SyntaxKind.IsExpression)
                {
                    if (_arity == 0 && node.Right is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        _logger.LogDebug("Replaced is expression type [teal]{identifierName}[/] with generic type", identifierName);

                        return Visit(node.WithRight(SyntaxFactory.GenericName(identifierName.Identifier, _arguments)));
                    }
                }

                return base.VisitBinaryExpression(node);
            }

            public override SyntaxNode? VisitSimpleBaseType(SimpleBaseTypeSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierSyntax && _types.Contains(identifierSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Replaced base type [teal]{node}[/] with generic type", node);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierSyntax.Identifier.WithTrailingTrivia(), _arguments.WithTrailingTrivia(identifierSyntax.Identifier.TrailingTrivia))));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Replaced base type [teal]{node}[/] with generic type", node);

                    return Visit(node.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(
                                _arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.Arguments[^1].GetTrailingTrivia()))));
                }

                return base.VisitSimpleBaseType(node);
            }

            public override SyntaxNode? VisitBaseList(BaseListSyntax node)
            {
                var result = node.Types;
                bool anyReplaced = false;

                for (int i = 0; i < node.Types.Count; i++)
                {
                    if (_arity == 0 && node.Types[i].Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                    {
                        result = result.Replace(result[i], node.Types[i].WithType(SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));

                        anyReplaced = true;
                    }
                    else if (_arity > 0 && node.Types[i].Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                    {
                        result = result.Replace(result[i], node.Types[i].WithType(
                            genericName.WithTypeArgumentList(
                                genericName.TypeArgumentList.AddArguments(
                                    _arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.Arguments[^1].GetTrailingTrivia()))));

                        anyReplaced = true;
                    }
                }

                if (anyReplaced)
                {
                    _logger.LogDebug("Made base type [teal]{baseTypes}[/] generic", node.Types);

                    return Visit(node.WithTypes(result));
                }

                return base.VisitBaseList(node);
            }
            public override SyntaxNode? VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node)
            {
                if (_arity == 0 && node.Name is IdentifierNameSyntax identifierNameSyntax && _types.Contains(identifierNameSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Made explicit interface implementation for [green]{interface}[/] generic", node.Name);

                    return Visit(node.WithName(SyntaxFactory.GenericName(identifierNameSyntax.Identifier.WithTrailingTrivia(), _arguments).WithTrailingTrivia(identifierNameSyntax.Identifier.TrailingTrivia)));
                }

                return base.VisitExplicitInterfaceSpecifier(node);
            }

            public override SyntaxNode? VisitArrayType(ArrayTypeSyntax node)
            {
                if (_arity == 0 && node.ElementType is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made array element type  [green]{elementType}[/] generic", node.ElementType);

                    return Visit(node.WithElementType(SyntaxFactory.GenericName(identifierName.Identifier, _arguments)));
                }

                return base.VisitArrayType(node);
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (_arity == 0 && node.ReturnType is IdentifierNameSyntax simpleName && _types.Contains(simpleName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made return type [green]{returnType}[/] generic", simpleName.Identifier);

                    node = node.WithReturnType(SyntaxFactory.GenericName(simpleName.Identifier.WithTrailingTrivia(), _arguments.WithTrailingTrivia(simpleName.Identifier.TrailingTrivia)));

                    return Visit(node);
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitTypeConstraint(TypeConstraintSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made type constraint [green]{typeConstraint}[/] generic", identifierName.Identifier);

                    node = node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(), _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia)));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made type constraint [green]{typeConstraint}[/] generic", genericName.Identifier);

                    node = node.WithType(genericName.WithTypeArgumentList(
                        genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                        .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia())));
                }

                return base.VisitTypeConstraint(node);
            }

            

            public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (_arity == 0 && node.Declaration.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made field [teal]{field}[/] generic", node);

                    return Visit(node.WithDeclaration(node.Declaration.WithType(
                        SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia)))));
                }
                else if (_arity > 0 && node.Declaration.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made field [teal]{field}[/] generic", node);

                    return Visit(node.WithDeclaration(node.Declaration.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia())))));
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made property [teal]{field}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(
                        SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made property [teal]{field}[/] generic", genericName.Identifier);

                    return Visit(node.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitPropertyDeclaration(node);
            }


            public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made indexer return type [teal]{returnType}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(
                        SyntaxFactory.GenericName(
                            identifierName.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }
                else if (_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made indexer return type  [teal]{returnType}[/] generic", genericName.Identifier);

                    return Visit(node.WithType(
                        genericName.WithTypeArgumentList(
                            genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                            .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitIndexerDeclaration(node);
            }

            public override SyntaxNode? VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                if (_arity == 0 && node.Type is IdentifierNameSyntax identifierNameSyntax && _types.Contains(identifierNameSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Made typeof expression [teal]{expr}[/] generic", node);

                    return Visit(node.WithType(
                        SyntaxFactory.GenericName(
                            identifierNameSyntax.Identifier.WithTrailingTrivia(),
                            _arguments.WithTrailingTrivia(identifierNameSyntax.Identifier.TrailingTrivia))));
                }
                else if(_arity > 0 && node.Type is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && _arity == genericName.Arity)
                {
                    if(genericName.TypeArgumentList.Arguments.OfType<OmittedTypeArgumentSyntax>().Any())
                    {
                        _logger.LogDebug("Added parameters to open generic typeof expression [teal]{expr}[/]", node);

                        var omittedArguments = Enumerable.Range(0, _parameters.Parameters.Count).Select(_ => SyntaxFactory.OmittedTypeArgument()).ToArray();

                        return Visit(node.WithType(
                            genericName.WithTypeArgumentList(
                                genericName.TypeArgumentList.AddArguments(omittedArguments)
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                    }
                    else
                    {
                        _logger.LogDebug("Added parameters to generic typeof expression [teal]{expr}[/]", node);

                        return Visit(node.WithType(
                            genericName.WithTypeArgumentList(
                                genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                    }
                }

                return base.VisitTypeOfExpression(node);
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (_arity == 0 && node.Expression is IdentifierNameSyntax identifierSyntax && _types.Contains(identifierSyntax.Identifier.ToString()))
                {
                    if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not TypeDeclarationSyntax typeDeclaration ||
                        (!hasPropertyOfName(typeDeclaration, identifierSyntax.Identifier.ToString()) &&
                         !hasFieldOfName(typeDeclaration, identifierSyntax.Identifier.ToString())))
                    {
                        _logger.LogDebug("Made access of type [teal]{identifier}[/] generic", identifierSyntax.Identifier);

                        return Visit(
                            node.WithExpression(
                                SyntaxFactory.GenericName(identifierSyntax.Identifier, _arguments)));
                    }

                    static bool hasPropertyOfName(TypeDeclarationSyntax typeDeclaration, string name) =>
                        typeDeclaration.Members.OfType<PropertyDeclarationSyntax>().Any(t => t.Identifier.ToString().Equals(name));

                    static bool hasFieldOfName(TypeDeclarationSyntax typeDeclaration, string name) =>
                        typeDeclaration.Members.OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Variables.Any(v => v.Identifier.ToString().Equals(name)));

                }
                else if (_arity > 0 && node.Expression is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made access of generic type [teal]{identifier}[/] generic", genericName.Identifier);

                    return Visit(
                        node.WithExpression(
                            genericName.WithTypeArgumentList(
                                genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitMemberAccessExpression(node);
            }
           
            public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                if(_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made variable declaration [teal]{identifier}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }
                
                return base.VisitVariableDeclaration(node);
            }

            public override SyntaxNode? VisitTypePattern(TypePatternSyntax node)
            {
                if(_arity == 0 && node.Type is IdentifierNameSyntax identifierName && _types.Contains(identifierName.Identifier.ToString()))
                {
                    _logger.LogDebug("Made type in pattern [teal]{identifier}[/] generic", identifierName.Identifier);

                    return Visit(node.WithType(SyntaxFactory.GenericName(identifierName.Identifier.WithTrailingTrivia(),
                        _arguments.WithTrailingTrivia(identifierName.Identifier.TrailingTrivia))));
                }

                return base.VisitTypePattern(node);
            }

            

            public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
            {
                if (_arity == 0 && node.Left is IdentifierNameSyntax identifierSyntax && _types.Contains(identifierSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Made qualified name [teal]{identifier}[/] generic", identifierSyntax.Identifier);

                    return Visit(
                        node.WithLeft(
                            SyntaxFactory.GenericName(identifierSyntax.Identifier, _arguments)));
                }
                else if (_arity > 0 && node.Left is GenericNameSyntax genericName && _types.Contains(genericName.Identifier.ToString()) && genericName.Arity == _arity)
                {
                    _logger.LogDebug("Made qualified name [teal]{identifier}[/] generic", genericName.Identifier);

                    return Visit(
                        node.WithLeft(
                            genericName.WithTypeArgumentList(
                                genericName.TypeArgumentList.AddArguments(_arguments.Arguments.ToArray())
                                .WithTrailingTrivia(genericName.TypeArgumentList.GetTrailingTrivia()))));
                }

                return base.VisitQualifiedName(node);
            }

            public override SyntaxNode? VisitNameMemberCref(NameMemberCrefSyntax node)
            {
                if (_arity == 0 && node.Name is IdentifierNameSyntax identifierNameSyntax && _types.Contains(identifierNameSyntax.Identifier.ToString()))
                {
                    _logger.LogDebug("Made cref expression [teal]{expr}[/] generic", node);

                    return Visit(node.WithName(SyntaxFactory.GenericName(identifierNameSyntax.Identifier,
                        _arguments
                            .WithLessThanToken(SyntaxFactory.Token(default, SyntaxKind.LessThanToken, "{", "{", default))
                            .WithGreaterThanToken(SyntaxFactory.Token(default, SyntaxKind.GreaterThanToken, "}", "}", default)))));
                }

                return base.VisitNameMemberCref(node);
            }
        }
    }
}
