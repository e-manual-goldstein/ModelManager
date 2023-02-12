﻿using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Reflection;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec
    {
        #region Null Spec

        public static TypeSpec NullSpec = CreateNullSpec();

        private static TypeSpec CreateNullSpec()
        {
            var spec = new TypeSpec("null", null, new List<IRule>());
            spec.Exclude("Null Spec");
            spec.SkipProcessing("Null Spec");
            spec.IsNullSpec = true;
            return spec;
        }

        #endregion

        private string _fullTypeName;
        public bool IsInterface { get; }
        public bool IsSystemType { get; }

        public TypeSpec(string typeName, bool isInterface, AssemblySpec assembly, ISpecManager specManager, List<IRule> rules) : this(typeName, specManager, rules)
        {
            IsInterface = isInterface;
            Assembly = assembly;
            IsSystemType = AssemblyLoader.IsSystemAssembly(assembly.FilePath);
        }

        public TypeSpec(string fullTypeName, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _fullTypeName = fullTypeName;
        }

        protected override void BuildSpec()
        {
            _specManager.TryBuildTypeSpecForAssembly(_fullTypeName, Assembly, type =>
            {
                BaseSpec = CreateBaseSpec(type);
                Interfaces = CreateInterfaceSpecs(type);
                Properties = CreatePropertySpecs(type);
                Methods = CreateMethodSpecs(type);
                Fields = CreateFieldSpecs(type);
                ProcessGenerics(type);
            });
        }

        private TypeSpec CreateBaseSpec(Type type)
        {
            if (_specManager.TryLoadTypeSpec(() => type.BaseType, out TypeSpec typeSpec))
            {
                typeSpec.AddSubType(this);
            }
            return typeSpec;
        }

        private TypeSpec[] CreateInterfaceSpecs(Type type)
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

        private PropertySpec[] CreatePropertySpecs(Type type)
        {
            var specs = _specManager.TryLoadPropertySpecs(() => type.GetProperties().Where(m => m.DeclaringType == type).ToArray(), this);
            return specs;
        }

        private MethodSpec[] CreateMethodSpecs(Type type)
        {
            var specs = _specManager.TryLoadMethodSpecs(() => type.GetMethods().Where(m => m.DeclaringType == type)
                .Except(Properties.SelectMany(p => p.InnerMethods())).ToArray());
            return specs;
        }

        private FieldSpec[] CreateFieldSpecs(Type type)
        {
            var specs = _specManager.TryLoadFieldSpecs(() => type.GetFields().Where(m => m.DeclaringType == type).ToArray(), this);
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
        
        public AssemblySpec Assembly { get; }

        private List<TypeSpec> _implementations = new List<TypeSpec>();

        public TypeSpec[] Implementations => _implementations.ToArray();

        public AssemblySpec[] GetDependentAssemblies()
        {
            return Implementations.Select(i => i.Assembly)
                .Concat(ReturnTypeSpecs.Select(r => r.DeclaringType.Assembly)).Distinct().ToArray();
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

        #region Generic Type Flags

        private void ProcessGenerics(Type type)
        {
            IsGenericType = type.IsGenericType;
            IsGenericParameter = type.IsGenericParameter;
            IsGenericTypeDefinition = type.IsGenericTypeDefinition;
            ContainsGenericParameters = type.ContainsGenericParameters;
            IsGenericTypeParameter = type.IsGenericTypeParameter;
        }

        public bool IsGenericType { get; private set; }

        public bool IsGenericParameter { get; private set; }

        public bool IsGenericTypeDefinition { get; private set; }

        public bool ContainsGenericParameters { get; private set; }

        public bool IsGenericTypeParameter { get; private set; }

        #endregion

        public bool IsNullSpec { get; private set; }

        public override string ToString()
        {
            return _fullTypeName;
        }

        List<IMemberSpec> _returnTypeSpecs = new List<IMemberSpec>();
        public IMemberSpec[] ReturnTypeSpecs => _returnTypeSpecs.ToArray();

        public void RegisterAsReturnType(IMemberSpec methodSpec)
        {
            if (!_returnTypeSpecs.Contains(methodSpec))
            {
                _returnTypeSpecs.Add(methodSpec);
            }
        }
    }
}
