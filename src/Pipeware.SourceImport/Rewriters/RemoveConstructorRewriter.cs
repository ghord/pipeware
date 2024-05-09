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
    public class RemoveConstructorRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Parameters { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ParameterCount { get; set; }

        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Multiple { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (ParameterCount is not null && Parameters is not null)
            {
                context.Logger.LogError("Method rewriter should have either arguments or argumentCount set, not both");

                return tree;
            }

            var arguments = Parameters != null ? Parameters.Split(",") : ParameterCount != null ? new string[ParameterCount.Value] : null;

            var rewriter = new RemoveConstructorCSharpRewriter(Type, arguments, Multiple, context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            if (!rewriter.Removed)
            {
                context.Logger.LogWarning("Failed to remove .ctor");

                return tree;
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveConstructorCSharpRewriter : CSharpSyntaxRewriter
        {
            private string? _type;
            private string?[]? _parameters;
            private bool _multiple;
            private ILogger _logger;

            public bool Removed { get; private set; }

            public RemoveConstructorCSharpRewriter(string? type, string?[]? parameters, bool multiple, ILogger logger)
            {
                _type = type;
                _parameters = parameters;
                _multiple = multiple;
                _logger = logger;
            }

            public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                if (_type is null || node.Identifier.ToString().Equals(_type))
                {
                    if (_parameters is null)
                    {
                        MarkRemoved(node);

                        return null;
                    }

                    if (node.ParameterList.Parameters.Count != _parameters.Length)
                    {
                        return base.VisitConstructorDeclaration(node);
                    }

                    for (int i = 0; i < node.ParameterList.Parameters.Count; i++)
                    {
                        if (_parameters[i] is null)
                            continue;

                        if (!node.ParameterList.Parameters[i].Identifier.ToString().Equals(_parameters[i]))
                        {
                            return base.VisitConstructorDeclaration(node);
                        }
                    }

                    MarkRemoved(node);

                    return null;
                }

                return base.VisitConstructorDeclaration(node);
            }

            private void MarkRemoved(ConstructorDeclarationSyntax node)
            {
                if(Removed && !_multiple)
                {
                    _logger.LogWarning("Constructor {ctor} declaration was removed multiple times", node.Identifier);
                }

                _logger.LogDebug("Removed .ctor [teal]{ctor}{params}[/]", node.Identifier, node.ParameterList);

                Removed = true;
            }
        }

    }
}
