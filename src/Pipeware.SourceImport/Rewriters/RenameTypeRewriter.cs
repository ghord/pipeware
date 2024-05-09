using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RenameTypeRewriter : IImportRewriter
    {
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? Renames { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetNamespace { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceNamespace { get; set; }

        [JsonIgnore]
        public bool IsDefault => TargetNamespace == null && SourceNamespace == null;

        public static RenameTypeRewriter CreateDefault()
        {
            return new RenameTypeRewriter { Renames = new Dictionary<string, JsonElement>() };
        }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Renames == null || Renames.Count == 0)
            {
                context.Logger.LogError("Missing type renames in type rewriter");

                return tree;
            }

            foreach (var (sourceName, targetNameJson) in Renames)
            {
                if (targetNameJson.ValueKind != JsonValueKind.String)
                {
                    context.Logger.LogWarning("Target name for {sourceName} is not a string in type rewriter", sourceName);

                    continue;
                }

                var targetName = targetNameJson.GetString()!;

                var rewriter = new RenameTypeCSharpRewriter(sourceName, targetName, SourceNamespace, context.Logger);

                var renamedTree = tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);

                if (rewriter.DeclarationChanged && IsPathDependentOnTypeName(tree.FilePath, sourceName, targetName, out var suggestedPath))
                {
                    renamedTree = renamedTree.WithFilePath(suggestedPath);

                    context.Logger.LogDebug("Changed target path to [green]{targetPath}[/] due to class rename", tree.FilePath);
                }

                if (renamedTree != tree)
                {
                    var visitor = new GetNamespaceCSharpVisitor();
                    visitor.Visit(renamedTree.GetRoot());

                    var fullNamespace = visitor.Namespace;

                    if (TargetNamespace != null && !TargetNamespace.Equals(fullNamespace))
                    {
                        tree = new AddUsingRewriter { Using = TargetNamespace }.Rewrite(context, renamedTree);
                    }
                    else
                    {
                        tree = renamedTree;
                    }
                }
            }

            return tree;
        }

        public static bool IsPathDependentOnTypeName(string targetPath, string sourceName)
        {
            return IsPathDependentOnTypeName(targetPath, sourceName, sourceName, out _);
        }

        public static bool IsPathDependentOnTypeName(string targetPath, string sourceName, string targetName, [NotNullWhen(true)] out string? suggestedPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(targetPath);

            if (sourceName.Equals(fileName))
            {
                suggestedPath = Path.Combine(Path.GetDirectoryName(targetPath)!, targetName + ".cs");
                return true;
            }
            else if ((sourceName + "OfT").Equals(fileName))
            {
                suggestedPath = Path.Combine(Path.GetDirectoryName(targetPath)!, targetName + "OfT.cs");
                return true;
            }
            else if (fileName.IndexOf(".") is int idx && idx > -1 && fileName.Substring(0, idx).Equals(fileName))
            {
                suggestedPath = Path.Combine(Path.GetDirectoryName(targetPath)!, targetName + fileName.Substring(idx + 1) + ".cs");
                return true;
            }

            suggestedPath = null;
            return false;
        }

        public void AddRename(string sourceType, string targetType)
        {
            if (Renames is null)
                Renames = new Dictionary<string, JsonElement>();

            // Seriously... No method to convert string to JsonElement
            var data = Encoding.UTF8.GetBytes("\"" + targetType + "\"");
            var reader = new Utf8JsonReader(data.AsSpan());

            Renames[sourceType] = JsonElement.ParseValue(ref reader);
        }

        class GetNamespaceCSharpVisitor : CSharpSyntaxWalker
        {
            private string? _namespace;

            public string? Namespace => _namespace;

            public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                _namespace ??= node.Name.ToString();
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                _namespace ??= node.Name.ToString();
            }
        }

        class RenameTypeCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _sourceName;
            private string _targetName;
            private ILogger _logger;
            private string? _sourceNamespace;

            public bool DeclarationChanged { get; private set; }

            public RenameTypeCSharpRewriter(string sourceName, string targetName, string? sourceNamespace, ILogger logger) : base(true)
            {
                _sourceName = sourceName;
                _targetName = targetName;
                _logger = logger;
                _sourceNamespace = sourceNamespace;
            }

            public override SyntaxNode? VisitNameMemberCref(NameMemberCrefSyntax node)
            {
                if (node.Name.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced cref [teal]{type}[/] with type [green]{target}[/]", node.Name, _targetName);

                    return node.WithName(SyntaxFactory.ParseTypeName(_targetName));
                }
                else if (node.Name is GenericNameSyntax genericType && genericType.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced cref [teal]{type}[/] with type [green]{target}[/]", genericType, _targetName);

                    return node.WithName(genericType.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericType.Identifier)));
                }

                return base.VisitNameMemberCref(node);
            }

            public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
            {
                if (node.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced cast [teal]{type}[/] with type [green]{target}[/]", node.Type, _targetName);

                    return Visit(node.WithType(SyntaxFactory.ParseTypeName(_targetName).WithTriviaFrom(node.Type)));
                }

                return base.VisitCastExpression(node);
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (_sourceNamespace is not null && node.Expression.ToString().Equals(_sourceNamespace + "." + _sourceName))
                {
                    _logger.LogDebug("Replaced identifier [teal]{identifier}[/] with type [green]{target}[/]", node.Expression, _targetName);

                    return Visit(
                        node.WithExpression(
                            SyntaxFactory.IdentifierName(_targetName).WithTriviaFrom(node.Expression)));
                }

                if (node.Expression is IdentifierNameSyntax identifierSyntax && identifierSyntax.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced identifier [teal]{identifier}[/] with type [green]{target}[/]", identifierSyntax.Identifier, _targetName);

                    return Visit(
                        node.WithExpression(
                            identifierSyntax.WithIdentifier(
                                SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierSyntax.Identifier))));
                }

                return base.VisitMemberAccessExpression(node);
            }



            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (node.Type != null)
                {
                    if (node.Type is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced parameter [teal]{node}[/] with type [green]{target}[/]", node, _targetName);

                        return Visit(node.WithType(identifierNameSyntax.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierNameSyntax.Identifier))));
                    }
                    else if (node.Type is GenericNameSyntax genericType && genericType.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced parameter [teal]{node}[/] with type [green]{target}[/]", node, _targetName);

                        return Visit(node.WithType(genericType.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericType.Identifier))));
                    }

                }

                return base.VisitParameter(node);
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    DeclarationChanged = true;

                    _logger.LogDebug("Replaced class declaration [teal]{class}[/] with type [green]{target}[/]", node.Identifier, _targetName);

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(node.Identifier)));
                }

                return base.VisitClassDeclaration(node);
            }

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    DeclarationChanged = true;

                    _logger.LogDebug("Replaced interface declaration [teal]{interface}[/] with type [green]{target}[/]", node.Identifier, _targetName);

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(node.Identifier)));
                }

                return base.VisitInterfaceDeclaration(node);
            }

            public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced .ctor declaration [teal]{ctor}[/] with type [green]{target}[/]", node.Identifier, _targetName);

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_targetName)));
                }

                return base.VisitConstructorDeclaration(node);
            }

            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (node.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced constructor expression [teal]{type}[/] with type [green]{target}[/]", node.Type, _targetName);

                    return Visit(node.WithType(SyntaxFactory.ParseTypeName(_targetName).WithTriviaFrom(node.Type)));
                }

                return base.VisitObjectCreationExpression(node);
            }


            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.ReturnType is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced return type of method [teal]{method}[/] with type [green]{target}[/]", node.Identifier, _targetName);

                    return Visit(node.WithReturnType(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName)).WithTriviaFrom(node.ReturnType)));
                }
                else if (node.ReturnType is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced generic return type of method [teal]{method}[/] with type [green]{target}[/]", node.Identifier, _targetName);

                    return Visit(node.WithReturnType(genericName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericName.Identifier))));
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitBaseList(BaseListSyntax node)
            {
                var types = node.Types;

                bool anyReplaced = false;

                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i].Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced base list element [teal]{type}[/] with type [green]{target}[/]", types[i], _targetName);

                        types = types.Replace(types[i], types[i].WithType(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));

                        anyReplaced = true;
                    }
                    else if (types[i].Type is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced base list element [teal]{type}[/] with type [green]{target}[/]", types[i], _targetName);

                        types = types.Replace(types[i], types[i].WithType(genericName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericName.Identifier))));

                        anyReplaced = true;
                    }
                }

                if (anyReplaced)
                {
                    return Visit(node.WithTypes(types));
                }
                else
                {
                    return base.VisitBaseList(node);
                }
            }

            public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.Kind() == SyntaxKind.AsExpression)
                {
                    if (node.Right is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced as expression [teal]{expr}[/] with type [green]{target}[/]", node.Right, _targetName);

                        return Visit(node.WithRight(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));
                    }
                    else if (node.Right is GenericNameSyntax genericType && genericType.Identifier.ToString().Equals(_sourceName))
                    {
                        _logger.LogDebug("Replaced as expression [teal]{expr}[/] with generic type [green]{target}[/]", node.Right, _targetName);

                        return Visit(node.WithRight(genericType.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericType.Identifier))));
                    }
                }

                return base.VisitBinaryExpression(node);
            }

            public override SyntaxNode? VisitTypeConstraint(TypeConstraintSyntax node)
            {
                if (node.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced type constraint [teal]{constraint}[/] with type [green]{target}[/]", node.Type, _targetName);

                    return node.WithType(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier)));
                }
                else if (node.Type is GenericNameSyntax genericType && genericType.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced generic type constraint [teal]{constraint}[/] with type [green]{target}[/]", node.Type, _targetName);

                    return node.WithType(genericType.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericType.Identifier)));
                }

                return base.VisitTypeConstraint(node);
            }

            public override SyntaxNode? VisitTypeParameter(TypeParameterSyntax node)
            {
                if (node.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced type parameter [teal]{param}[/] with type [green]{target}[/]", node, _targetName);

                    return node.WithIdentifier(SyntaxFactory.Identifier(_targetName));
                }

                return base.VisitTypeParameter(node);
            }

            public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.Declaration.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced field [teal]{field}[/] with type [green]{target}[/]", node, _targetName);

                    return Visit(node.WithDeclaration(node.Declaration.WithType(SyntaxFactory.ParseTypeName(_targetName).WithTriviaFrom(node.Declaration.Type))));
                }

                return base.VisitFieldDeclaration(node);
            }
            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced property [teal]{property}[/] with type [green]{target}[/]", node, _targetName);

                    return Visit(node.WithType(SyntaxFactory.ParseTypeName(_targetName).WithTriviaFrom(node.Type)));
                }

                return base.VisitPropertyDeclaration(node);
            }
            public override SyntaxNode? VisitNullableType(NullableTypeSyntax node)
            {
                if (node.ElementType is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced nullable type [teal]{node}[/] with type [green]{target}[/]", node, _targetName);

                    return Visit(node.WithElementType(identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(identifierName.Identifier))));
                }
                else if (node.ElementType is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced nullable generic type [teal]{node}[/] with type [green]{target}[/]", node, _targetName);

                    return Visit(node.WithElementType(genericName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericName.Identifier))));
                }

                return base.VisitNullableType(node);
            }

            public override SyntaxNode? VisitSimpleBaseType(SimpleBaseTypeSyntax node)
            {
                if (node.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced base type [teal]{node}[/] with type [green]{target}[/]", node, _targetName);

                    return Visit(node.WithType(SyntaxFactory.ParseTypeName(_targetName).WithTriviaFrom(node.Type)));
                }

                return base.VisitSimpleBaseType(node);
            }


            public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
            {
                var result = node.Arguments;
                bool anyReplaced = false;

                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    if (node.Arguments[i] is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_sourceName))
                    {
                        result = result.Replace(result[i], identifierName.WithIdentifier(SyntaxFactory.Identifier(_targetName)));
                        anyReplaced = true;
                    }
                    else if (node.Arguments[i] is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_sourceName))
                    {
                        result = result.Replace(result[i], genericName.WithIdentifier(SyntaxFactory.Identifier(_targetName)));
                        anyReplaced = true;
                    }
                }

                if (anyReplaced)
                {
                    _logger.LogDebug("Replaced generic type arguments [teal]{args}[/] with [green]{result}[/]", node.Arguments, result);

                    return Visit(node.WithArguments(result));
                }

                return base.VisitTypeArgumentList(node);
            }

            public override SyntaxNode? VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                if (node.Type.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced [teal]{node}[/] expression with [green]typeof({target})[/]", node, _targetName);

                    return Visit(node.WithType(SyntaxFactory.ParseTypeName(_targetName)));
                }
                else if (node.Type is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_sourceName))
                {
                    _logger.LogDebug("Replaced [teal]{node}[/] expression with type [green]typeof({target})[/]", node, _targetName);

                    return Visit(node.WithType(genericName.WithIdentifier(SyntaxFactory.Identifier(_targetName).WithTriviaFrom(genericName.Identifier))));
                }

                return base.VisitTypeOfExpression(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals("nameof"))
                {
                    if (node.ArgumentList.Arguments.Count == 1)
                    {
                        var expression = node.ArgumentList.Arguments[0].Expression;

                        if (expression is IdentifierNameSyntax identifier)
                        {
                            if (identifier.ToString().Equals(_sourceName))
                            {
                                _logger.LogDebug("Replaced [teal]{node}[/] expression with [green]nameof({target})[/]", node, _targetName);

                                return node.WithArgumentList(
                                    node.ArgumentList.WithArguments(
                                        node.ArgumentList.Arguments.Replace(
                                            node.ArgumentList.Arguments[0],
                                            node.ArgumentList.Arguments[0].WithExpression(
                                                SyntaxFactory.ParseTypeName(_targetName)
                                            ))));
                            }
                        }
                        else if (expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression is IdentifierNameSyntax memberIdentifier)
                        {
                            if (memberIdentifier.ToString().Equals(_sourceName))
                            {
                                _logger.LogDebug("Replaced [teal]{node}[/] expression with [green]{target}[/] type", node, _targetName);

                                return node.WithArgumentList(
                                    node.ArgumentList.WithArguments(
                                        node.ArgumentList.Arguments.Replace(
                                            node.ArgumentList.Arguments[0],
                                            node.ArgumentList.Arguments[0].WithExpression(
                                                memberAccess.WithExpression(
                                                    SyntaxFactory.ParseTypeName(_targetName))
                                            ))));
                            }
                        }
                    }
                }

                return base.VisitInvocationExpression(node);
            }


        }
    }
}
