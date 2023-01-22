using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class TypeSpec
    {
        private Type _type;

        public TypeSpec(Type type)
        {
            _type = type;
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            Interfaces = analyser.LoadTypeSpecs(_type.GetInterfaces());
            BaseSpec = analyser.LoadTypeSpec(_type.BaseType);
            Properties = analyser.LoadPropertySpecs(_type.GetProperties());
            Methods = analyser.LoadMethodSpecs(_type.GetMethods().Except(Properties.SelectMany(p => p.InnerMethods())).ToArray());
            await BeginAnalysis(analyser);
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            var interfaces = AnalyseInterfaces(analyser);
            await Task.WhenAll(interfaces);
        }

        private Task AnalyseInterfaces(Analyser analyser)
        {
            return Task.Run(() => Array.ForEach(Interfaces, async i => await i.AnalyseAsync(analyser)));
        }

        public TypeSpec[] Interfaces { get; private set; }

        public TypeSpec BaseSpec { get; private set; }

        public MethodSpec[] Methods { get; private set; }

        public PropertySpec[] Properties { get; set; }

        public override string ToString()
        {
            return _type.FullName;
        }
    }
}
