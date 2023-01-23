using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class TypeSpec : ISpec
    {
        private Type _type;
        private string _typeName;
        private bool _analysing;
        private bool _analysed;
        private bool _analysable;
        public TypeSpec(Type type) : this(type.FullName)
        {
            _type = type;
            _analysable = true;
        }

        public TypeSpec(string typeName)
        {
            _typeName = typeName;
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (_analysable && !_analysed && !_analysing)
            {
                _analysing = true;
                Interfaces = analyser.LoadTypeSpecs(_type.GetInterfaces());
                BaseSpec = analyser.LoadTypeSpec(_type.BaseType);
                Properties = analyser.LoadPropertySpecs(_type.GetProperties());
                Methods = analyser.LoadMethodSpecs(_type.GetMethods().Except(Properties.SelectMany(p => p.InnerMethods())).ToArray());
                Fields = analyser.LoadFieldSpecs(_type.GetFields()).ToArray();
                await BeginAnalysis(analyser);                
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task baseSpec = AnalyseBaseSpec(analyser);
            Task interfaces = AnalyseInterfaces(analyser);
            Task properties = AnalyseProperties(analyser);
            Task methods = AnalyseMethods(analyser);
            await Task.WhenAll(baseSpec, interfaces, properties, methods);
            _analysed = true;
        }

        private Task AnalyseBaseSpec(Analyser analyser)
        {
            return Task.Run(() => (BaseSpec != null) ? BaseSpec.AnalyseAsync(analyser) : Task.CompletedTask);
        }

        private Task AnalyseInterfaces(Analyser analyser)
        {
            return Task.WhenAll(Interfaces.Select(i => i.AnalyseAsync(analyser)));
        }

        private Task AnalyseProperties(Analyser analyser)
        {
            return Task.WhenAll(Properties.Select(p => p.AnalyseAsync(analyser)));
        }

        private Task AnalyseMethods(Analyser analyser)
        {
            return Task.WhenAll(Methods.Select(m => m.AnalyseAsync(analyser)));            
        }


        public TypeSpec[] Interfaces { get; private set; }

        public TypeSpec BaseSpec { get; private set; }

        public MethodSpec[] Methods { get; private set; }

        public PropertySpec[] Properties { get; set; }
        
        public FieldSpec[] Fields { get; set; }

        public override string ToString()
        {
            return _typeName;
        }
    }
}
