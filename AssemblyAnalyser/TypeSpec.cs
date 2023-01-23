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
            ExclusionRules = new List<ExclusionRule<TypeSpec>>();
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (_analysable && !_analysed && !_analysing)
            {
                _analysing = true;
                Assembly = analyser.LoadAssemblySpec(_type.Assembly);
                Interfaces = analyser.LoadTypeSpecs(_type.GetInterfaces());
                BaseSpec = analyser.TryLoadTypeSpec(() => _type.BaseType);
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
            Task fields = AnalyseFields(analyser);
            await Task.WhenAll(baseSpec, interfaces, properties, methods, fields);
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

        private Task AnalyseFields(Analyser analyser)
        {
            return Task.WhenAll(Fields.Select(f => f.AnalyseAsync(analyser)));
        }
        
        public AssemblySpec Assembly { get; private set; }

        public TypeSpec[] Interfaces { get; private set; }

        public TypeSpec BaseSpec { get; private set; }

        public MethodSpec[] Methods { get; private set; }

        public PropertySpec[] Properties { get; private set; }
        
        public FieldSpec[] Fields { get; private set; }

        public override string ToString()
        {
            return _typeName;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<TypeSpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<TypeSpec>> InclusionRules { get; private set; }
    }
}
