using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests
{
    [TestClass]
    public class GlobbingTests
    {

        [TestMethod]
        public void ShouldMatcherMatchRElative()
        {
            var matcher = new Matcher();
            
            matcher.AddInclude("**/*.cs");
            matcher.AddExclude("**/Sync*.cs");

            var file = Path.Combine(Environment.CurrentDirectory, @"../../SyncPipeline.cs");

            Assert.IsTrue(matcher.Match(Path.GetPathRoot(Environment.CurrentDirectory), file).HasMatches);
        }
    }
}
