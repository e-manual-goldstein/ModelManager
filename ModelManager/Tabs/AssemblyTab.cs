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

        public string AnalyseAssembly()
        {
            //var assemblyPath = @"D:\Goldstein\Cosmos\Cosmos.Server\bin\Debug\netcoreapp3.1\Cosmos.Server.dll";
            //var interfaces = new List<Type>();
            var analyser = new Analyser(@"D:\Goldstein\Cosmos\Cosmos.Server\bin\Debug\netcoreapp3.1\");

            analyser.SpecRules.Add(CommonRules.IncludeByAssemblyName("Cosmos.Model"));
            analyser.BeginAsync();
            //var assemblySpec = analyser.LoadAssemblySpec(Assembly.LoadFrom(assemblyPath));
            //assemblySpec.AnalyseAsync(analyser);
            return analyser.Report();
        }
    }
}
