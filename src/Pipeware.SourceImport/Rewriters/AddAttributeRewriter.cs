using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class AddAttributeRewriter : IImportRewriter
    {
        public required string Attribute { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Setter { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TypeParameter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Parameter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }


        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Setter is null && TypeParameter is null && Parameter is null && Method is null)
            {
                context.Logger.LogWarning("Missing target specifier for add attribute rewriter");

                return tree;
            }
            
            var rewriter = new AddAttributeCSharpRewriter(Attribute, Setter, Method, Parameter, TypeParameter,  context.Logger);
            var root = rewriter.Visit(tree.GetRoot());

            if(!rewriter.Added)
            {
                context.Logger.LogWarning("Attribute {Attribute} not added", Markup.Escape(Attribute));
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class AddAttributeCSharpRewriter : CSharpSyntaxRewriter
        {
            private readonly string _attributeName;
            private readonly AttributeListSyntax _attributeList;
            private readonly string? _typeParameter;
            private readonly string? _parameter;
            private readonly string? _method;
            private readonly string? _setter;
            private readonly ILogger _logger;

            public bool Added { get; private set; }

            public AddAttributeCSharpRewriter(string attribute, string? setter, string? method, string? parameter, string? typeParameter,  ILogger logger)
            {

                if(attribute.IndexOf('(') > -1)
                {
                    var attributeType = attribute.Substring(0, attribute.IndexOf('('));
                    var argumentList = attribute.Substring(attribute.IndexOf('('));

                    _attributeName = attributeType;
                    _attributeList = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName(attributeType),
                                SyntaxFactory.ParseAttributeArgumentList(argumentList) ?? throw new Exception("Cannot parse attributes"))));
                }
                else
                {
                    _attributeName = attribute;
                    _attributeList = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.ParseName(attribute))));
                }

                _typeParameter = typeParameter;
                _setter = setter;
                _logger = logger;
                _parameter = parameter;
                _method = method;
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_setter))
                {
                    var accessors = node.AccessorList;

                    if (TryGetSetter(node.AccessorList, out var setter))
                    {
                        if (!ContainsAttribute(setter.AttributeLists))
                        {
                            _logger.LogDebug("Adding attribute {Attribute} to type parameter {TypeParameter}.", Markup.Escape(_attributeName), _setter);

                            Added = true;

                            return Visit(node.ReplaceNode(setter,
                                setter.WithLeadingTrivia(SyntaxFactory.Whitespace(" ")).WithAttributeLists(
                                    node.AttributeLists.Add(
                                        _attributeList
                                            .WithLeadingTrivia(setter.GetLeadingTrivia())))));
                                            
                        }
                    }
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_method))
                {
                    if (!ContainsAttribute(node.AttributeLists))
                    {
                        _logger.LogDebug("Adding attribute {Attribute} to method {Method}.", Markup.Escape(_attributeName), _method);

                        Added = true;

                        return Visit(node.WithAttributeLists(
                            node.AttributeLists.Add(
                                _attributeList
                                    .WithLeadingTrivia(node.GetLeadingTrivia()))));
                    }
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitTypeParameter(TypeParameterSyntax node)
            {
                if(node.Identifier.ToString().Equals(_typeParameter))
                {
                    if(!ContainsAttribute(node.AttributeLists))
                    {
                        _logger.LogDebug("Adding attribute {Attribute} to type parameter {TypeParameter}.", Markup.Escape(_attributeName), _typeParameter);

                        Added = true;

                        return Visit(node.WithAttributeLists(
                            node.AttributeLists.Add(
                                _attributeList
                                    .WithLeadingTrivia(node.GetLeadingTrivia()))));
                    }
                }
                return base.VisitTypeParameter(node);
            }

            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (node.Identifier.ToString().Equals(_parameter))
                {
                    if (!ContainsAttribute(node.AttributeLists))
                    {
                        _logger.LogDebug("Adding attribute {Attribute} to parameter {TypeParameter}.", Markup.Escape(_attributeName), _typeParameter);

                        Added = true;

                        return Visit(node.WithAttributeLists(
                            node.AttributeLists.Add(
                                _attributeList
                                    .WithLeadingTrivia(node.GetLeadingTrivia()))));
                    }
                }

                return base.VisitParameter(node);
            }

            private bool TryGetSetter(AccessorListSyntax? accessorList, [NotNullWhen(true)] out AccessorDeclarationSyntax? setter)
            {
                if(accessorList is null)
                {
                    setter = null;
                    return false;
                }

                foreach(var accessor in accessorList.Accessors)
                {
                    if(accessor.Keyword.Text == "set")
                    {
                        setter = accessor;
                        return true;
                    }
                }

                setter = null;
                return false;
            }

            private bool ContainsAttribute(SyntaxList<AttributeListSyntax> attributeLists)
            {
                foreach (var attributeList in attributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (attribute.Name is IdentifierNameSyntax identifierName && identifierName.Identifier.ToString().Equals(_attributeName))
                        {
                            return true;
                        }
                        else if (attribute.Name is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_attributeName))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

        }
    }
}
