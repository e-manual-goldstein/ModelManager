﻿using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using AssemblyAnalyser.Specs;
using System;

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

        TypeDefinition _typeDefinition;
        TypeReference _typeReference;
        public TypeSpec(TypeReference typeReference, ISpecManager specManager, List<IRule> rules)
            : this($"{typeReference.Namespace}.{ typeReference.Name}", typeReference.FullName, specManager, rules)
        {
            _typeReference = typeReference;
            _typeDefinition = (typeReference is TypeDefinition typeDefinition) ? typeDefinition : null;
            Name = typeReference.Name;
            Namespace = typeReference.Namespace;
            if (_typeDefinition == null)
            {
                specManager.AddFault($"Could not find matching TypeDefinition for {FullTypeName}");
            }
            IsInterface = _typeDefinition?.IsInterface;
            IsSystemType = Module?.IsSystem;
        }

        TypeSpec(string fullTypeName, string uniqueTypeName, ISpecManager specManager, List<IRule> rules) 
            : base(rules, specManager)
        {
            UniqueTypeName = uniqueTypeName;
            FullTypeName = fullTypeName;
        }

        public string UniqueTypeName { get; }
        public string FullTypeName { get; }
        public string Namespace { get; set; }
        public bool? IsInterface { get; private set; }
        public bool? IsSystemType { get; }

        #region BuildSpec

        protected override void BuildSpec()
        {
            if (FullTypeName != null && _typeDefinition != null)
            {
                BuildSpecInternal();
            }
            else
            {
                Logger.LogError("Cannot build Spec with null FullTypeName");
            }
        }

        protected void BuildSpecInternal()
        {
            _baseSpec = CreateBaseSpec();
            _interfaces = CreateInterfaceSpecs();
            _nestedTypes = CreateNestedTypeSpecs();
            _fields = CreateFieldSpecs();
            _methods = CreateMethodSpecs();
            _properties = CreatePropertySpecs();
            _events = CreateEventSpecs();
            _attributes = _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this);
            ProcessInterfaceImplementations();
            ProcessCompilerGenerated();
            ProcessGenerics();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _typeDefinition.CustomAttributes.ToArray();
        }

        private void ProcessCompilerGenerated()
        {
            IsCompilerGenerated = _typeDefinition.CustomAttributes.OfType<CompilerGeneratedAttribute>().Any();
            if (IsCompilerGenerated)
            {
                if (_typeDefinition.DeclaringType != null)
                {
                    //TODO
                    //DeclaringType = type.DeclaringType;
                }
                else
                {

                }
            }
        }

        ModuleSpec _module;
        public ModuleSpec Module => _module ??= _specManager.LoadReferencedModuleByScopeName(_typeReference.Module, _typeReference.Scope);

        TypeSpec _baseSpec;
        public TypeSpec BaseSpec => _baseSpec ??= CreateBaseSpec();

        private TypeSpec CreateBaseSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _typeDefinition.BaseType, out TypeSpec typeSpec))
            {
                if (!typeSpec.IsNullSpec)
                {
                    typeSpec.AddSubType(this);
                }
            }
            return typeSpec;
        }

        TypeSpec[] _interfaces;
        public TypeSpec[] Interfaces => _interfaces ??= CreateInterfaceSpecs();

        private TypeSpec[] CreateInterfaceSpecs()
        {
            if (_specManager.TryLoadTypeSpecs(() => _typeDefinition.Interfaces.Select(i => i.InterfaceType).ToArray(), out TypeSpec[] specs))
            {
                foreach (var interfaceSpec in specs.Where(s => !s.IsNullSpec))
                {
                    interfaceSpec.AddImplementation(this);
                }
            }
            return specs;
        }

        TypeSpec[] _nestedTypes;
        public TypeSpec[] NestedTypes => _nestedTypes ??= CreateNestedTypeSpecs();

        private TypeSpec[] CreateNestedTypeSpecs()
        {
            if (_specManager.TryLoadTypeSpecs(() => _typeDefinition.NestedTypes.Where(n => n.DeclaringType == _typeDefinition).ToArray()
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

        MethodSpec[] _methods;
        public MethodSpec[] Methods => _methods ??= CreateMethodSpecs();

        private MethodSpec[] CreateMethodSpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault($"Unable to determine MethodSpecs for {this}");
                return Array.Empty<MethodSpec>();
            }
            var specs = _specManager.TryLoadMethodSpecs(() => _typeDefinition.Methods.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        PropertySpec[] _properties;
        public PropertySpec[] Properties => _properties ??= CreatePropertySpecs();

        private PropertySpec[] CreatePropertySpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault($"Unable to determine PropertySpecs for {this}");
                return Array.Empty<PropertySpec>();
            }
            var specs = _specManager.TryLoadPropertySpecs(() => _typeDefinition.Properties.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        public PropertySpec GetPropertySpec(string name)
        {
            return _properties.Where(p => p.Name == name).SingleOrDefault();
        }

        FieldSpec[] _fields;
        public FieldSpec[] Fields => _fields ??= CreateFieldSpecs();

        private FieldSpec[] CreateFieldSpecs()
        {
            var specs = _specManager.TryLoadFieldSpecs(() => _typeDefinition.Fields.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        EventSpec[] _events;
        public EventSpec[] Events => _events ??= CreateEventSpecs();

        private EventSpec[] CreateEventSpecs()
        {
            var specs = _specManager.TryLoadEventSpecs(() => _typeDefinition.Events.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        TypeSpec[] _genericTypeParamters;
        public TypeSpec[] GenericTypeParameters => _genericTypeParamters;

        #region Generic Type Flags

        private void ProcessGenerics()
        {
            //IsGenericType = _typeDefinition.IsGenericInstance;
            //IsGenericTypeDefinition = _typeDefinition.IsGenericInstance; // This seems to be never unequal to IsGenericType
            //if (IsGenericType)
            //{
            //    var genericTypes = new List<TypeSpec>();
            //    foreach (var parameterType in type.GenericTypeParameters)
            //    {
            //        if (_specManager.TryLoadTypeSpec(() => parameterType, out TypeSpec typeSpec))
            //        {
            //            genericTypes.Add(typeSpec);
            //            typeSpec.BuildSpec(parameterType.GetTypeInfo());
            //        }
            //    }
            //    GenericTypeParameters = genericTypes.ToArray();
            //}            
            //if (type.IsGenericParameter)
            //{

            //}
            //if (type.IsGenericTypeParameter)
            //{

            //}
            //IsGenericParameter = type.IsGenericParameter;
            //IsGenericTypeParameter = type.IsGenericTypeParameter;
        }

        public bool IsGenericType { get; private set; }

        public bool IsGenericParameter { get; private set; }

        public bool IsGenericTypeDefinition { get; private set; }

        public bool ContainsGenericParameters { get; private set; }

        public bool IsGenericTypeParameter { get; private set; }

        #endregion

        private void ProcessInterfaceImplementations()
        {
            if (IsInterface != true) // Skip unless explicitly labelled as NOT an interface
            {
                foreach (var interfaceSpec in Interfaces)
                {
                    RegisterMemberImplementations(interfaceSpec);
                }
            }
        }

        private void RegisterMemberImplementations(TypeSpec interfaceSpec)
        {
            foreach (var interfaceProperty in interfaceSpec.Properties)
            {
                var propertySpec = GetPropertySpec(interfaceProperty.Name);
                if (propertySpec == null)
                {
                    _specManager.AddFault($"{this} does not implement {interfaceProperty}");
                }
                else
                {
                    propertySpec.Implements = interfaceProperty;
                }
            }
            foreach (var interfaceMethod in interfaceSpec.Methods)
            {
                var methodSpec = MatchMethodSpecByNameAndParameterType(interfaceMethod.Name, interfaceMethod.Parameters);
                if (methodSpec == null)
                {
                    _specManager.AddFault($"{this} does not implement {interfaceMethod}");
                }
                else
                {
                    methodSpec.Implements = methodSpec;
                }
            }
        }

        public bool IsNullSpec { get; private set; }
        public bool IsErrorSpec { get; private set; }
        public bool IsCompilerGenerated { get; private set; }

        #endregion
        
        public ModuleSpec[] GetDependentModules()
        {
            return Implementations.Select(i => i.Module)
                .Concat(ResultTypeSpecs.Select(r => r.DeclaringType.Module)).Distinct().ToArray();
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

        public override string ToString()
        {
            return $"{_typeReference.Namespace}_{_typeReference.FullName}" ?? UniqueTypeName;
        }
        
        private List<TypeSpec> _implementations = new List<TypeSpec>();

        public TypeSpec[] Implementations => _implementations.ToArray();

        public void AddImplementation(TypeSpec typeSpec)
        {
            if (IsInterface.Equals(false))
            {
                throw new InvalidOperationException("Cannot implement a non-interface Type");
            }
            if (!_implementations.Contains(typeSpec))
            {
                _implementations.Add(typeSpec);
                RegisterDependentTypeForModule(typeSpec);
            }
        }

        private List<TypeSpec> _subTypes = new List<TypeSpec>();

        public void AddSubType(TypeSpec typeSpec)
        {
            if (!_subTypes.Contains(typeSpec))
            {
                _subTypes.Add(typeSpec);
                RegisterDependentTypeForModule(typeSpec);
            }
        }

        public TypeSpec[] GetSubTypes() => _subTypes.ToArray();

        List<IMemberSpec> _resultTypeSpecs = new List<IMemberSpec>();
        public IMemberSpec[] ResultTypeSpecs => _resultTypeSpecs.ToArray();

        public void RegisterAsResultType(IMemberSpec methodSpec)
        {
            if (!_resultTypeSpecs.Contains(methodSpec))
            {
                _resultTypeSpecs.Add(methodSpec);
                RegisterDependentTypeForModule(methodSpec.DeclaringType);
            }
        }

        List<ParameterSpec> _dependentParameterSpecs = new List<ParameterSpec>();
        public ParameterSpec[] DependentParameterSpecs => _dependentParameterSpecs.ToArray();

        public void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            if (!_dependentParameterSpecs.Contains(parameterSpec))
            {
                _dependentParameterSpecs.Add(parameterSpec);
                RegisterDependentTypeForModule(parameterSpec.Method.DeclaringType);
            }
        }

        List<MethodSpec> _dependentMethodBodies = new List<MethodSpec>();
        public MethodSpec[] DependentMethodBodies => _dependentMethodBodies.ToArray();

        public void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            if (!_dependentMethodBodies.Contains(methodSpec))
            {
                _dependentMethodBodies.Add(methodSpec);
                RegisterDependentTypeForModule(methodSpec.DeclaringType);
            }
        }

        List<AbstractSpec> _decoratorForSpecs = new List<AbstractSpec>();

        public AbstractSpec[] DecoratorForSpecs => _decoratorForSpecs.ToArray();        

        public void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            if (!_decoratorForSpecs.Contains(decoratedSpec))
            {
                _decoratorForSpecs.Add(decoratedSpec);
                //TODO Finish this part
                //Assembly.RegisterDependentType(decoratedSpec);
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

        List<EventSpec> _delegateFor = new List<EventSpec>();

        public EventSpec[] DelegateForSpecs => _delegateFor.ToArray();

        public bool HasDefinition => _typeDefinition != null;
        
        public void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            if (!_delegateFor.Contains(eventSpec))
            {
                _delegateFor.Add(eventSpec);
                RegisterDependentTypeForModule(eventSpec.DeclaringType);
            }
        }

        public void AddDefinition(TypeDefinition type)
        {
            _typeDefinition = type;
        }

        public bool IsSpecFor(TypeReference typeReference, bool moduleChecked = false)
        {
            if (moduleChecked || Module.IsSpecFor(typeReference))
            {
                return typeReference.FullName == _typeDefinition?.FullName;
            }
            else
            {

            }
            return false;
        }

        public MethodSpec GetMethodSpec(MethodReference method)
        {
            var matchingMethods = Methods.Where(m => m.IsSpecFor(method)).ToList();
            if (matchingMethods.Count > 1)
            {

            }
            return Methods.SingleOrDefault(m => m.IsSpecFor(method));
        }

        public MethodSpec[] GetMethodSpecs(string methodName)
        {
            return Methods.Where(m => m.Name == methodName).ToArray();
        }

        public MethodSpec MatchMethodSpecByNameAndParameterType(string methodName, ParameterSpec[] parameterSpecs)
        {
            var matchingMethods = Methods.Where(m
                    => m.Name == methodName
                    && m.Parameters.Length == parameterSpecs.Length
                    && m.HasExactParameterTypes(parameterSpecs));
            return matchingMethods.SingleOrDefault();
        }

        private void RegisterDependentTypeForModule(TypeSpec typeSpec)
        {
            if (Module == null)
            {
                Logger.LogWarning($"Module not found for Type: {_typeReference}");
                return;
            }
            Module.RegisterDependentType(typeSpec);
        }
    }
}
