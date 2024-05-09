using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class LicenseRewriter : IImportRewriter
    {
        private string _hash;
        private string _remoteFileLocation;
        private string? _alias;
        private const string SourceFileComment = "// Source file: ";
        private const string SourceHashComment = "// Source Sha256: ";
        private const string SourceAliasComment = "// Source alias: ";

        public LicenseRewriter(string hash, string remoteFileLocation, string? alias)
        {
            _hash = hash;
            _remoteFileLocation = remoteFileLocation;
            _alias = alias;
        }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"// Copyright (c) {DateTime.Now.Year} Grzegorz Hordyński. All rights reserved.");
            sb.AppendLine("// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.");
            sb.AppendLine();
            sb.AppendLine(SourceFileComment + _remoteFileLocation);
            if (_alias != null)
                sb.AppendLine(SourceAliasComment + _alias);
            sb.AppendLine(SourceHashComment + _hash);
            sb.AppendLine();
            sb.AppendLine("// Originally licensed under:");
            sb.AppendLine();

            sb.Append(tree.GetRoot().GetLeadingTrivia().ToFullString());

            return tree.WithRootAndOptions(tree.GetRoot().WithLeadingTrivia(SyntaxFactory.Comment(sb.ToString())), tree.Options);

        }

        public static bool TryExtractInformation(ILogger logger, string targetFile, [NotNullWhen(true)] out string? sourceFile, [NotNullWhen(true)] out string? sourceHash, out string? alias)
        {
            sourceFile = sourceHash = alias = null;

            try
            {
                using var file = File.OpenRead(targetFile);
                using var reader = new StreamReader(file);


                while (reader.ReadLine() is string line)
                {
                    if (line.StartsWith(SourceFileComment, StringComparison.Ordinal))
                    {
                        sourceFile = line.Substring(SourceFileComment.Length);
                    }
                    else if (line.StartsWith(SourceHashComment, StringComparison.Ordinal))
                    {
                        sourceHash = line.Substring(SourceHashComment.Length);
                    }
                    else if (line.StartsWith(SourceAliasComment, StringComparison.Ordinal))
                    {
                        alias = line.Substring(SourceAliasComment.Length);
                    }
                }

                if (sourceFile != null && sourceHash != null)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while extracting hash from file '{targetFile}'.", targetFile);
                return false;
            }

        }
    }
}
