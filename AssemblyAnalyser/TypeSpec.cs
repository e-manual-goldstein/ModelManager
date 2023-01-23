using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec
    {
        #region Null Spec

        public static TypeSpec NullSpec = CreateNullSpec();

        private static TypeSpec CreateNullSpec()
        {
            var spec = new TypeSpec("null", new List<IRule>());
            spec.Exclude();
            spec.SkipProcessing();
            return spec;
        }

        #endregion

        private Type _type;
        private string _typeName;
        
        public TypeSpec(Type type, List<IRule> rules) : this(type.FullName, rules)
        {
            _type = type;
        }

        public TypeSpec(string typeName, List<IRule> rules) : base(rules)
        {
            _typeName = typeName;
        }

        protected override void BeginProcessing(Analyser analyser)
        {
            Assembly = analyser.LoadAssemblySpec(_type.Assembly);
            Interfaces = analyser.LoadTypeSpecs(_type.GetInterfaces());
            BaseSpec = analyser.TryLoadTypeSpec(() => _type.BaseType);
            Properties = analyser.LoadPropertySpecs(_type.GetProperties());
            Methods = analyser.LoadMethodSpecs(_type.GetMethods().Except(Properties.SelectMany(p => p.InnerMethods())).ToArray());
            Fields = analyser.LoadFieldSpecs(_type.GetFields()).ToArray();            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task baseSpec = AnalyseBaseSpec(analyser);
            Task interfaces = AnalyseInterfaces(analyser);
            Task properties = AnalyseProperties(analyser);
            Task methods = AnalyseMethods(analyser);
            Task fields = AnalyseFields(analyser);
            await Task.WhenAll(baseSpec, interfaces, properties, methods, fields);            
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
        
        public AssemblySpec Assembly { get; set; } 

        public TypeSpec[] Interfaces { get; private set; }

        public TypeSpec BaseSpec { get; private set; }

        public MethodSpec[] Methods { get; private set; }

        public PropertySpec[] Properties { get; private set; }
        
        public FieldSpec[] Fields { get; private set; }

        public override string ToString()
        {
            return _typeName;
        }
    }
}
