using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec
    {
        #region Null Spec

        public static TypeSpec NullSpec = CreateNullSpec();

        private static TypeSpec CreateNullSpec()
        {
            var spec = new TypeSpec("null", "nullspec", null, new List<IRule>());
            spec.Exclude("Null Spec");
            spec.SkipProcessing("Null Spec");
            spec.IsNullSpec = true;
            return spec;
        }

        #endregion

        #region Error Spec
        static int error_count = 1;
        public static TypeSpec CreateErrorSpec(string reason)
        {
            var spec = new TypeSpec("ErrorSpec", $"{reason}{error_count++}", null, new List<IRule>());
            spec.Exclude(reason);
            spec.SkipProcessing(reason);
            spec.IsErrorSpec = true;
            return spec;
        }


        #endregion

        public string UniqueTypeName { get; }
        public string FullTypeName { get; }
        public string Namespace { get; set; }
        public string Name { get; set; }
        public bool IsInterface { get; }
        public bool IsSystemType { get; }
        
        public TypeSpec(string typeName, string uniqueTypeName, bool isInterface, AssemblySpec assembly, ISpecManager specManager, List<IRule> rules)
            : this(typeName, uniqueTypeName, specManager, rules)
        {
            IsInterface = isInterface;
            Assembly = assembly;
            IsSystemType = AssemblyLoader.IsSystemAssembly(assembly.FilePath);
        }

        TypeSpec(string fullTypeName, string uniqueTypeName, ISpecManager specManager, List<IRule> rules) 
            : base(rules, specManager)
        {
            UniqueTypeName = uniqueTypeName;
            FullTypeName = fullTypeName;
        }

        protected override void BuildSpec()
        {
            _specManager.TryBuildTypeSpecForAssembly(FullTypeName, Namespace, Name, Assembly, type =>
            {
                BuildSpec(type);
            });
        }

        protected void BuildSpec(TypeInfo type)
        {
            BaseSpec = CreateBaseSpec(type);
            Interfaces = CreateInterfaceSpecs(type);
            NestedTypes = CreateNestedTypeSpecs(type);
            Fields = CreateFieldSpecs(type);
            Methods = CreateMethodSpecs(type);
            Properties = CreatePropertySpecs(type);
            Attributes = _specManager.TryLoadAttributeSpecs(() => GetAttributes(type), this);
            ProcessCompilerGenerated(type);
            ProcessGenerics(type);
        }

        private CustomAttributeData[] GetAttributes(Type type)
        {
            return type.GetCustomAttributesData().ToArray();
        }

        private void ProcessCompilerGenerated(TypeInfo type)
        {
            IsCompilerGenerated = type.HasAttribute(typeof(CompilerGeneratedAttribute));
            if (IsCompilerGenerated)
            {
                if (type.DeclaringType != null)
                {
                    //TODO
                    //DeclaringType = type.DeclaringType;
                }
                else
                {

                }
            }
        }

        private TypeSpec CreateBaseSpec(TypeInfo type)
        {
            if (_specManager.TryLoadTypeSpec(() => type.BaseType, out TypeSpec typeSpec))
            {
                if (!typeSpec.IsNullSpec)
                {
                    typeSpec.AddSubType(this);
                }                
            }
            return typeSpec;
        }

        private TypeSpec[] CreateInterfaceSpecs(TypeInfo type)
        {
            if (_specManager.TryLoadTypeSpecs(() => type.GetInterfaces(), out TypeSpec[] specs))
            {
                foreach (var interfaceSpec in specs.Where(s => !s.IsNullSpec))
                {
                    interfaceSpec.AddImplementation(this);
                }
            }
            return specs;
        }

        private TypeSpec[] CreateNestedTypeSpecs(TypeInfo type)
        {
            if (_specManager.TryLoadTypeSpecs(() => type.GetNestedTypes().Where(n => n.DeclaringType == type).ToArray()
                , out TypeSpec[] specs))
            {
                foreach (var nestedType in specs.Where(s => !s.IsNullSpec))
                {
                    nestedType.SetNestedIn(this);
                    nestedType.Process();
                }
            }
            return specs;
        }

        private MethodSpec[] CreateMethodSpecs(TypeInfo type)
        {
            var specs = _specManager.TryLoadMethodSpecs(() => type.GetMethods().Where(m => m.DeclaringType == type).ToArray(), this);
            return specs;
        }

        private PropertySpec[] CreatePropertySpecs(TypeInfo type)
        {
            var specs = _specManager.TryLoadPropertySpecs(() => type.GetProperties().Where(m => m.DeclaringType == type).ToArray(), this);
            return specs;
        }

        private FieldSpec[] CreateFieldSpecs(TypeInfo type)
        {
            var specs = _specManager.TryLoadFieldSpecs(() => type.GetFields().Where(m => m.DeclaringType == type).ToArray(), this);
            return specs;
        }

        #region Defunct
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
        #endregion

        public AssemblySpec Assembly { get; }

        private List<TypeSpec> _implementations = new List<TypeSpec>();

        public TypeSpec[] Implementations => _implementations.ToArray();

        public AssemblySpec[] GetDependentAssemblies()
        {
            return Implementations.Select(i => i.Assembly)
                .Concat(ResultTypeSpecs.Select(r => r.DeclaringType.Assembly)).Distinct().ToArray();
        }

        public void AddImplementation(TypeSpec typeSpec)
        {
            if (!IsInterface)
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
        
        public TypeSpec[] NestedTypes { get; private set; }

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

        public TypeSpec[] GenericTypeParameters { get; private set; }

        #region Generic Type Flags

        private void ProcessGenerics(TypeInfo type)
        {
            IsGenericType = type.IsGenericType;
            IsGenericTypeDefinition = type.IsGenericTypeDefinition; // This seems to be never unequal to IsGenericType
            if (IsGenericType)
            {
                var genericTypes = new List<TypeSpec>();
                foreach (var parameterType in type.GenericTypeParameters)
                {
                    if (_specManager.TryLoadTypeSpec(() => parameterType, out TypeSpec typeSpec))
                    {
                        genericTypes.Add(typeSpec);
                        typeSpec.BuildSpec(parameterType.GetTypeInfo());
                    }
                }
                GenericTypeParameters = genericTypes.ToArray();
            }            
            if (type.IsGenericParameter)
            {

            }
            if (type.IsGenericTypeParameter)
            {

            }
            IsGenericParameter = type.IsGenericParameter;
            IsGenericTypeParameter = type.IsGenericTypeParameter;
        }

        public bool IsGenericType { get; private set; }

        public bool IsGenericParameter { get; private set; }

        public bool IsGenericTypeDefinition { get; private set; }

        public bool ContainsGenericParameters { get; private set; }

        public bool IsGenericTypeParameter { get; private set; }

        #endregion

        public bool IsNullSpec { get; private set; }
        public bool IsErrorSpec { get; private set; }

        public bool IsCompilerGenerated { get; private set; }

        public override string ToString()
        {
            return FullTypeName ?? UniqueTypeName;
        }

        List<IMemberSpec> _resultTypeSpecs = new List<IMemberSpec>();
        public IMemberSpec[] ResultTypeSpecs => _resultTypeSpecs.ToArray();

        public void RegisterAsResultType(IMemberSpec methodSpec)
        {
            if (!_resultTypeSpecs.Contains(methodSpec))
            {
                _resultTypeSpecs.Add(methodSpec);
            }
        }

        List<ParameterSpec> _dependentParameterSpecs = new List<ParameterSpec>();
        public ParameterSpec[] DependentParameterSpecs => _dependentParameterSpecs.ToArray();

        public void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            if (!_dependentParameterSpecs.Contains(parameterSpec))
            {
                _dependentParameterSpecs.Add(parameterSpec);
            }
        }

        List<MethodSpec> _dependentMethodBodies = new List<MethodSpec>();
        public MethodSpec[] DependentMethodBodies => _dependentMethodBodies.ToArray();

        public void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            if (!_dependentMethodBodies.Contains(methodSpec))
            {
                _dependentMethodBodies.Add(methodSpec);
            }
        }

        public TypeSpec NestedIn { get; private set; }

        private void SetNestedIn(TypeSpec typeSpec)
        {
            if (NestedIn != null)
            {
                Logger.LogError($"NestedIn already set for Type {this}");
            }
            NestedIn = typeSpec;
        }

        List<AbstractSpec> _decoratorForSpecs = new List<AbstractSpec>();

        public AbstractSpec[] DecoratorForSpecs => _decoratorForSpecs.ToArray();

        public void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            if (!_decoratorForSpecs.Contains(decoratedSpec))
            {
                _decoratorForSpecs.Add(decoratedSpec);
            }
        }

        public string DecribeFields()
        {
            if (!Fields.Any())
            {
                return string.Empty;
            }
            return Fields.Select(f => f.FieldName).Aggregate((a, b) => a + ";" + b);
        }
    }
}
