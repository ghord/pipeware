using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public class ProjectImportSettings
    {
        public Dictionary<string, string> Namespaces { get; set; } = new Dictionary<string, string>();

        public List<FileRewrite> Files { get; set; } = new List<FileRewrite>();

        public List<RewriteRule> Rules { get; set; } = new List<RewriteRule>();
    }
  
}
