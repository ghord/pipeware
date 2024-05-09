using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class AddInterfaceRewriter : IImportRewriter
    {
        public required string Interface { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new AddInterfaceCSharpRewriter(Interface, Type, context.Logger);

            var result = rewriter.Visit(tree.GetRoot());

            if (!rewriter.Added)
            {
                context.Logger.LogWarning("Cannot add interface [teal]{interface}[/]", Markup.Escape(Interface));
            }

            return tree.WithRootAndOptions(result, tree.Options);
        }

        class AddInterfaceCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _interface;
            private ILogger _logger;
            private string? _type;
            private bool _added;

            public AddInterfaceCSharpRewriter(string @interface, string? type, ILogger logger)
            {
                _interface = @interface;
                _logger = logger;
                _type = type;
            }
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if(node.BaseList == null)
                {
                    if(_type == null || node.Identifier.ToString().Equals(_type))
                    {
                        return Visit(node.WithBaseList(SyntaxFactory.BaseList()));
                    }
                }

                return base.VisitClassDeclaration(node);
            }

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                if (node.BaseList == null)
                {
                    if (_type == null || node.Identifier.ToString().Equals(_type))
                    {
                        return Visit(node.WithBaseList(SyntaxFactory.BaseList()));
                    }
                }

                return base.VisitInterfaceDeclaration(node);
            }

            public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                if (node.BaseList == null)
                {
                    if (_type == null || node.Identifier.ToString().Equals(_type))
                    {
                        return Visit(node.WithBaseList(SyntaxFactory.BaseList()));
                    }
                }

                return base.VisitStructDeclaration(node);
            }

            public override SyntaxNode? VisitBaseList(BaseListSyntax node)
            {
                if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not TypeDeclarationSyntax typeDeclaration)
                {
                    return base.VisitBaseList(node);
                }

                bool shouldAdd = true;

                if (_type != null && !typeDeclaration.Identifier.ToString().Equals(_type))
                {
                    shouldAdd = false;
                }

                foreach (var type in node.Types)
                {
                    if (type.Type is IdentifierNameSyntax identifier && identifier.Identifier.ToString().Equals(_interface))
                    {
                        shouldAdd = false;
                        break;
                    }
                    else if (type.Type is GenericNameSyntax genericName && genericName.Identifier.ToString().Equals(_interface))
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    if(_added)
                    {
                        _logger.LogWarning("Interface {interface} added to more than one type", Markup.Escape(_interface));
                    }

                    _logger.LogDebug("Added interface [teal]{interface}[/] to [teal]{type}[/]", Markup.Escape(_interface), typeDeclaration.Identifier);
                    _added = true;

                    return Visit(node.AddTypes(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(_interface))
                            .WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
                            .WithTrailingTrivia(node.GetTrailingTrivia())));
                }

                return base.VisitBaseList(node);
            }
            public bool Added => _added;
        }

    }
}
