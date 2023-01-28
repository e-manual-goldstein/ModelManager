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
            spec.Exclude("Null Spec");
            spec.SkipProcessing("Null Spec");
            spec.IsNullSpec = true;
            return spec;
        }

        #endregion

        private Type _type;
        private string _typeName;
        bool _isInterface;

        public TypeSpec(Type type, List<IRule> rules) : this(type.FullName, rules)
        {
            _type = type;
            _isInterface = _type.IsInterface;
        }

        public TypeSpec(string typeName, List<IRule> rules) : base(rules)
        {
            _typeName = typeName;
        }

        protected override void BeginProcessing(Analyser analyser)
        {
            Assembly = analyser.LoadAssemblySpec(_type.Assembly);
            Interfaces = CreateInterfaceSpecs(analyser);
            BaseSpec = CreateBaseSpec(analyser);
            Properties = CreatePropertySpecs(analyser);
            Methods = CreateMethodSpecs(analyser);
            Fields = CreateFieldSpecs(analyser);
        }

        private TypeSpec[] CreateInterfaceSpecs(Analyser analyser)
        {
            var specs = analyser.TryLoadTypeSpecs(() => _type.GetInterfaces());
            foreach (var interfaceSpec in specs.Where(s => !s.IsNullSpec))
            {
                interfaceSpec.AddImplementation(this); 
            }
            return specs;
        }

        private TypeSpec CreateBaseSpec(Analyser analyser)
        {
            var typeSpec = analyser.TryLoadTypeSpec(() => _type.BaseType);
            if (typeSpec != null && typeSpec != NullSpec)
            {
                typeSpec.AddSubType(this);
            }
            return typeSpec;
        }

        private PropertySpec[] CreatePropertySpecs(Analyser analyser)
        {
            var specs = analyser.TryLoadPropertySpecs(() => _type.GetProperties());
            return specs;
        }

        private MethodSpec[] CreateMethodSpecs(Analyser analyser)
        {
            var specs = analyser.TryLoadMethodSpecs(() => 
                _type.GetMethods().Except(Properties.SelectMany(p => p.InnerMethods())).ToArray());
            return specs;
        }

        private FieldSpec[] CreateFieldSpecs(Analyser analyser)
        {
            var specs = analyser.TryLoadFieldSpecs(() => _type.GetFields());
            return specs;
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

        private List<TypeSpec> _implementations = new List<TypeSpec>();

        public void AddImplementation(TypeSpec typeSpec)
        {
            if (!_isInterface)
            {
                throw new InvalidOperationException("Cannot implement a non-interface Type");
            }
            if (!_implementations.Contains(typeSpec))
            {
                _implementations.Add(typeSpec);
            }
        }

        public TypeSpec[] Interfaces { get; private set; }

        public TypeSpec BaseSpec { get; private set; }
        
        private List<TypeSpec> _subTypes = new List<TypeSpec>();

        public void AddSubType(TypeSpec typeSpec)
        {
            if (!_subTypes.Contains(typeSpec))
            {
                _subTypes.Add(typeSpec);
            }
        }

        public TypeSpec[] GetSubTypes() => _subTypes.ToArray();

        public MethodSpec[] Methods { get; private set; }

        public PropertySpec[] Properties { get; private set; }
        
        public FieldSpec[] Fields { get; private set; }

        public bool IsNullSpec { get; private set; }

        public override string ToString()
        {
            return _typeName;
        }
    }
}
