using AssemblyAnalyser;
using ModelManager.Core;
using ModelManager.Types;
using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ModelManager.Tabs
{
    public class AssemblyTab : AbstractServiceTab
    {
        public override string Title
        {
            get
            {
                return "Assembly";
            }
        }

        public void AnalyseAssembly()
        {
            var assemblyPath = @"";
            var interfaces = new List<Type>();
            var analyser = new Analyser();
            var assemblySpec = new AssemblySpec(Assembly.LoadFrom(assemblyPath));
            assemblySpec.AnalyseAsync(analyser);
            var types = analyser.Types();
        }
    }
}
