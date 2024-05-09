using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RenameFileRewriter : IImportRewriter
    {
        public required string FileName { get; set; }
        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var targetPath = Path.Combine(
                    Path.GetDirectoryName(tree.FilePath)!,
                    FileName);

            context.Logger.LogDebug("Changed target path to [green]{targetPath}[/] due to file rename", targetPath);

            return tree.WithFilePath(targetPath);
        }
    }
}
