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
    public class RemoveFieldRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Field { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Fields { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Multiple { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Keep { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Fields == null && Field == null)
            {
                context.Logger.LogError("Field rewriter has neither fields nor field property set");

                return tree;
            }

            var rewriter = new RemoveFieldCSharpRewriter(Fields ?? [Field!], Multiple, Keep, context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            foreach (var field in rewriter.UnprocessedFields)
            {
                if (!Keep)
                {
                    context.Logger.LogWarning("Field {field} was not found", field);
                }
                else
                {
                    context.Logger.LogWarning("Field {field} was not removed", field);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveFieldCSharpRewriter : CSharpSyntaxRewriter
        {
            private HashSet<string> _fields;
            private bool _multiple;
            private ILogger _logger;
            private bool _keep;
            private HashSet<string> _unprocessedFields;

            public RemoveFieldCSharpRewriter(IEnumerable<string> fields, bool multiple, bool keep, ILogger logger)
            {
                _fields = fields.ToHashSet();
                _unprocessedFields = keep ? fields.ToHashSet() : [];
                _multiple = multiple;
                _logger = logger;
                _keep = keep;
            }

            public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.Declaration.Variables.Count == 1)
                {
                    var fieldName = node.Declaration.Variables[0].Identifier.ToString();

                    if (_fields.Contains(fieldName) ^ _keep)
                    {
                        if (_keep)
                        {
                            _logger.LogDebug("Removed field [teal]{fieldName}[/]", fieldName);
                            _unprocessedFields.Remove(fieldName);

                            return null;
                        }
                        else
                        {
                            if (!_unprocessedFields.Add(fieldName) && !_multiple)
                            {
                                _logger.LogWarning("Field {fieldName} removed more than once", fieldName);
                            }

                            _logger.LogDebug("Removed field [teal]{fieldName}[/]", fieldName);
                        }

                        return null;
                    }
                }

                return base.VisitFieldDeclaration(node);
            }

            public IEnumerable<string> UnprocessedFields => _fields.Except(_unprocessedFields);
        }
    }
}
