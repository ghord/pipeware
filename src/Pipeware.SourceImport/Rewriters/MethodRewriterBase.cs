using Microsoft.CodeAnalysis;
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
    public abstract class MethodRewriterBase : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Methods { get; set; }



        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ParameterCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? Arity { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int[]? Arities { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParameterConstraint[]? Parameters { get; set; }
        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Method == null && Methods == null)
            {
                context.Logger.LogError("Method rewriter has neither Methods nor Method property set");

                return tree;
            }

            var constraint = new MethodConstraint
            {
                Methods = new HashSet<string>(Methods ?? [Method!]),
                ParameterCount = ParameterCount,
                Type = Type,
                Arities = Arity != null ? Arities ?? [Arity.Value] : Arities,
                Parameters = Parameters
            };


            return Rewrite(context, constraint, tree);
        }

        public abstract SyntaxTree Rewrite(RewriterContext context, MethodConstraint constraint, SyntaxTree tree);

      
    }
}
