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
    public class RemoveAttributeRewriter : IImportRewriter
    {
        public required string Attribute { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; init; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Type == null)
            {
                context.Logger.LogInformation("Remove attribute rewriter does not have any property set");

                return tree;
            }

            var rewriter = new RemoveAttributeCSharpRewriter(Attribute, Type, context.Logger);  

            var newRoot = rewriter.Visit(tree.GetRoot());

            if (Type is not null && !rewriter.TypeRemoved)
            {
                context.Logger.LogWarning("Attribute [green]{Attribute}[/] not found in class [green]{Type}[/]", Attribute, Type);
            }

            return tree.WithRootAndOptions(newRoot, tree.Options);
        }

        class RemoveAttributeCSharpRewriter : CSharpSyntaxRewriter
        {
            private string? _type;
            private ILogger _logger;
            private string _attribute;
            private bool _typeRemoved = false;

            public RemoveAttributeCSharpRewriter(string attribute, string? type, ILogger logger)
            {
                _type = type;
                _logger = logger;
                _attribute = attribute;
            }


            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if(_type != null && node.Identifier.ToString().Equals(_type))
                {
                    if(node.AttributeLists.Any(a => a.Attributes.Any(a => a.Name.ToString().Equals(_attribute))))
                    {
                        _logger.LogDebug("Removed attribute [green]{attribute}[/] from class [green]{type}[/]", _attribute, _type);

                        _typeRemoved = true;
                        return node.RemoveNodes(node.AttributeLists.Where(a => a.Attributes.Any(a => a.Name.ToString().Equals(_attribute))), SyntaxRemoveOptions.KeepNoTrivia);
                    }
                }

                return base.VisitClassDeclaration(node);
            }

            public bool TypeRemoved => _typeRemoved;
        }
    }
}
