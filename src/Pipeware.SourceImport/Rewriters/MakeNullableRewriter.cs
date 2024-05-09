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
    public class MakeNullableRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Field { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Parameter { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Field == null && Parameter == null)
            {
                context.Logger.LogWarning("Field and Parameter are both null in nullable rewriter.");

                return tree;
            }

            var rewriter = new NullableCSharpRewriter(Field, Parameter, context.Logger);
            var root = rewriter.Visit(tree.GetRoot());

            if (Field is not null && !rewriter.MadeFieldNullable)
            {
                context.Logger.LogWarning("Field {Field} not found in nullable rewriter.", Field);
            }

            if (Parameter is not null && !rewriter.MadeParameterNullable)
            {
                context.Logger.LogWarning("Parameter {Parameter} not found in nullable rewriter.", Parameter);
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class NullableCSharpRewriter : CSharpSyntaxRewriter
        {
            private readonly string? _field;
            private readonly string? _parameter;
            private readonly ILogger _logger;

            public NullableCSharpRewriter(string? field, string? parameter, ILogger logger)
            {
                _field = field;
                _parameter = parameter;
                _logger = logger;
            }

            public bool MadeFieldNullable { get; private set; }

            public bool MadeParameterNullable { get; private set; }

            public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (_field != null && node.Declaration.Variables.Count == 1 && node.Declaration.Variables[0].Identifier.ToString().Equals(_field))
                {
                    if (node.Declaration.Type is not NullableTypeSyntax)
                    {
                        _logger.LogDebug("Making field {Field} nullable.", _field);

                        MadeFieldNullable = true;

                        return Visit(
                            node.WithDeclaration(
                                node.Declaration.WithType(
                                    SyntaxFactory.NullableType(node.Declaration.Type.WithoutTrivia()).WithTriviaFrom(node.Declaration.Type))));
                    }
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode? VisitParameter(ParameterSyntax node)
            {
                if (_parameter! != null && node.Identifier.ToString().Equals(_parameter) && node.Type != null)
                {
                    if (node.Type is not NullableTypeSyntax)
                    {
                        _logger.LogDebug("Making parameter {Parameter} nullable.", _parameter);

                        MadeParameterNullable = true;

                        return Visit(
                            node.WithType(
                                SyntaxFactory.NullableType(node.Type.WithoutTrivia()).WithTriviaFrom(node.Type)));
                    }
                }
                return base.VisitParameter(node);
            }
        }
    }
}
