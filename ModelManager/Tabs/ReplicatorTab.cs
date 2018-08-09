using ModelManager.Core;
using ModelManager.Replicator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Tabs
{
    public class ReplicatorTab : AbstractServiceTab
    {
        public override string Title => "Replicator";

        public void MockProjectFromAssembly(string filePath, bool createFiles)
        {
            var assembly = Assembly.Load(filePath);
            var codeFiles = Generate.CodeFileStringsFromAssembly(assembly);
            
        }
    }
}
