using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using System;
using System.Reflection;
using AssemblyAnalyser.Specs;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec
    {
        #region Null Spec

        public static TypeSpec NullSpec = CreateNullSpec();

        private static TypeSpec CreateNullSpec()
        {
            var spec = new TypeSpec("null", "nullspec", null);
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
            var spec = new TypeSpec("ErrorSpec", $"{reason}{error_count++}", null);
            spec.Exclude(reason);
            spec.SkipProcessing(reason);
            spec.IsErrorSpec = true;
            return spec;
        }


        #endregion

        TypeDefinition _typeDefinition;
        
        public TypeSpec(TypeDefinition typeDefinition, ISpecManager specManager)
            : this($"{typeDefinition.Namespace}.{ typeDefinition.Name}", typeDefinition.FullName, specManager)
        {
            _typeDefinition = typeDefinition;
            Name = typeDefinition.Name;
            Namespace = typeDefinition.Namespace;
            if (_typeDefinition == null)
            {
                specManager.AddFault(FaultSeverity.Warning, $"Could not find matching TypeDefinition for {FullTypeName}");
            }
            IsInterface = _typeDefinition?.IsInterface;
            IsSystem = Module?.IsSystem ?? true;
            IsClass = typeDefinition.IsClass;
        }

        protected TypeSpec(string fullTypeName, string uniqueTypeName, ISpecManager specManager) 
            : base(specManager)
        {
            UniqueTypeName = uniqueTypeName;
            FullTypeName = fullTypeName;
        }

        public string UniqueTypeName { get; }
        public string FullTypeName { get; }
        public string Namespace { get; set; }
        public bool? IsInterface { get; }
        public bool IsClass { get; }
        //public bool? IsSystemType { get; }
        public bool IsArray { get; }

        #region BuildSpec

        protected override void BuildSpec()
        {
            if (FullTypeName != null && _typeDefinition != null)
            {
                BuildSpecInternal();
            }
            else
            {
                _specManager.AddFault(FaultSeverity.Error, "Cannot build Spec with null FullTypeName");
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
            _attributes = CreateAttributSpecs();
            ProcessInterfaceImplementations();
            ProcessCompilerGenerated();
            ProcessGenerics();
        }

        protected virtual TypeSpec[] CreateAttributSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _typeDefinition.CustomAttributes.ToArray();
        }

        private void ProcessCompilerGenerated()
        {
            //Might be obsolete under Mono.Cecil
            IsCompilerGenerated = _typeDefinition?.CustomAttributes.OfType<CompilerGeneratedAttribute>().Any() ?? false;
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
        public ModuleSpec Module => _module ??= TryGetModule();

        protected virtual ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByScopeName(_typeDefinition.Module, _typeDefinition.Scope);
        }

        TypeSpec _baseSpec;
        public TypeSpec BaseSpec => _baseSpec ??= CreateBaseSpec();

        protected virtual TypeSpec CreateBaseSpec()
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

        protected virtual TypeSpec[] CreateInterfaceSpecs()
        {
            if (_specManager.TryLoadTypeSpecs(() => _typeDefinition.Interfaces.Select(i => i.InterfaceType).ToArray(), out TypeSpec[] specs))
            {
                foreach (var interfaceSpec in specs.Where(s => !s.IsNullSpec))
                {
                    if (interfaceSpec is GenericInstanceSpec genericInstanceSpec)
                    {
                        Module.AddGenericTypeImplementation(genericInstanceSpec);
                    }
                    else
                    {
                    }
                    interfaceSpec.AddImplementation(this);
                }
            }
            return specs;
        }

        TypeSpec[] _nestedTypes;
        public TypeSpec[] NestedTypes => _nestedTypes ??= CreateNestedTypeSpecs();

        protected virtual TypeSpec[] CreateNestedTypeSpecs()
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

        protected virtual MethodSpec[] CreateMethodSpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault(FaultSeverity.Warning, $"Unable to determine MethodSpecs for {this}");
                return Array.Empty<MethodSpec>();
            }
            var specs = _specManager.TryLoadMethodSpecs(() => _typeDefinition.Methods.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        PropertySpec[] _properties;
        public PropertySpec[] Properties => _properties ??= CreatePropertySpecs();

        protected virtual PropertySpec[] CreatePropertySpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault(FaultSeverity.Warning, $"Unable to determine PropertySpecs for {this}");
                return Array.Empty<PropertySpec>();
            }
            var specs = _specManager.TryLoadPropertySpecs(() => _typeDefinition.Properties.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        public PropertySpec GetPropertySpec(string name)
        {
            return Properties.Where(p => !p.Parameters.Any() && p.Name == name).SingleOrDefault();
        }

        public PropertySpec MatchPropertySpecByNameAndParameterType(string name, ParameterSpec[] parameterSpecs)
        {
            var matchingProperties = Properties.Where(p
                    => p.Name == name
                    && p.Parameters.Length == parameterSpecs.Length
                    && p.HasExactParameters(parameterSpecs));
            if (matchingProperties.Count() > 1)
            {
                var methodArray = matchingProperties.ToArray();
                _specManager.AddFault(FaultSeverity.Error, $"Multiple Properties found for signature. PropertyName:{name}");
                return null;
            }
            return matchingProperties.SingleOrDefault();            
        }

        FieldSpec[] _fields;
        public FieldSpec[] Fields => _fields ??= CreateFieldSpecs();

        protected virtual FieldSpec[] CreateFieldSpecs()
        {
            var specs = _specManager.TryLoadFieldSpecs(() => _typeDefinition.Fields.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        EventSpec[] _events;
        public EventSpec[] Events => _events ??= CreateEventSpecs();

        protected virtual EventSpec[] CreateEventSpecs()
        {
            var specs = _specManager.TryLoadEventSpecs(() => _typeDefinition.Events.Where(m => m.DeclaringType == _typeDefinition).ToArray(), this);
            return specs;
        }

        GenericParameterSpec[] _genericTypeParamters;
        public GenericParameterSpec[] GenericTypeParameters => _genericTypeParamters ??= CreateGenericTypeParameters();

        protected virtual GenericParameterSpec[] CreateGenericTypeParameters()
        {
            _specManager.TryLoadTypeSpecs(() => _typeDefinition.GenericParameters.ToArray(), out GenericParameterSpec[] typeSpecs);
            return typeSpecs;
        }


        #region Generic Type Flags

        private void ProcessGenerics()
        {
            if (IsGenericInstance)
            {

            }
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
            //IsGenericTypeParameter = type.IsGenericTypeParameter;
        }

        public virtual bool IsGenericInstance => false;
        public virtual bool IsGenericParameter => false;

        #endregion

        private void ProcessInterfaceImplementations()
        {
            if (IsInterface != true) // Skip unless explicitly labelled as NOT an interface
            {
                foreach (var interfaceSpec in Interfaces.Where(i => !i.IsGenericInstance))
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
                    _specManager.AddFault(FaultSeverity.Warning, $"{this} does not implement {interfaceProperty}");
                }
                else
                {
                    propertySpec.Implements = interfaceProperty;
                }
            }
            foreach (var interfaceMethod in interfaceSpec.Methods)
            {
                var methodSpec = MatchMethodSpecByNameAndParameterType(interfaceMethod.Name, interfaceMethod.Parameters
                    , interfaceMethod.GenericTypeArguments);
                if (methodSpec == null)
                {
                    _specManager.AddFault(FaultSeverity.Warning, $"{this} does not implement {interfaceMethod}");
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
                if (NestedIn != typeSpec)
                {
                    _specManager.AddFault(FaultSeverity.Error, $"NestedIn already set for Type {this}");
                }
                else
                {
                    _specManager.AddMessage($"NestedIn already set to this value: {typeSpec}");
                }
                return;
            }
            NestedIn = typeSpec;
        }

        public override string ToString()
        {
            return 
                //$"{_typeDefinition.Namespace}_{_typeDefinition.FullName}" ?? 
                UniqueTypeName;
        }
        
        private List<TypeSpec> _implementations = new List<TypeSpec>();

        public TypeSpec[] Implementations => _implementations.ToArray();

        public virtual void AddImplementation(TypeSpec typeSpec)
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
        public TypeSpec[] GetSubTypes => _subTypes.ToArray();
        public void AddSubType(TypeSpec typeSpec)
        {
            if (!_subTypes.Contains(typeSpec))
            {
                _subTypes.Add(typeSpec);
                RegisterDependentTypeForModule(typeSpec);
            }
        }

        List<IMemberSpec> _resultTypeSpecs = new List<IMemberSpec>();
        public IMemberSpec[] ResultTypeSpecs => _resultTypeSpecs.ToArray();

        public virtual void RegisterAsResultType(IMemberSpec methodSpec)
        {
            if (!_resultTypeSpecs.Contains(methodSpec))
            {
                _resultTypeSpecs.Add(methodSpec);
                RegisterDependentTypeForModule(methodSpec.DeclaringType);
            }
        }

        List<ParameterSpec> _dependentParameterSpecs = new List<ParameterSpec>();
        public ParameterSpec[] DependentParameterSpecs => _dependentParameterSpecs.ToArray();

        public virtual void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            if (!_dependentParameterSpecs.Contains(parameterSpec))
            {
                _dependentParameterSpecs.Add(parameterSpec);
                RegisterDependentTypeForModule(parameterSpec.Member.DeclaringType);
            }
        }

        List<MethodSpec> _dependentMethodBodies = new List<MethodSpec>();
        public MethodSpec[] DependentMethodBodies => _dependentMethodBodies.ToArray();

        public virtual void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            if (!_dependentMethodBodies.Contains(methodSpec))
            {
                _dependentMethodBodies.Add(methodSpec);
                RegisterDependentTypeForModule(methodSpec.DeclaringType);
            }
        }

        List<AbstractSpec> _decoratorForSpecs = new List<AbstractSpec>();

        public AbstractSpec[] DecoratorForSpecs => _decoratorForSpecs.ToArray();        

        public virtual void RegisterAsDecorator(AbstractSpec decoratedSpec)
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
        
        public virtual void RegisterAsDelegateFor(EventSpec eventSpec)
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

        public MethodSpec MatchMethodSpecByNameAndParameterType(string methodName, ParameterSpec[] parameterSpecs
            , GenericParameterSpec[] genericTypeArgumentSpecs)
        {
            var matchingMethods = Methods.Where(m
                    => m.Name == methodName
                    && m.Parameters.Length == parameterSpecs.Length
                    && m.HasExactParameters(parameterSpecs)
                    && m.HasExactGenericTypeArguments(genericTypeArgumentSpecs)
                    );
            if (matchingMethods.Count() > 1)
            {
                var methodArray = matchingMethods.ToArray();
                _specManager.AddFault(FaultSeverity.Error, $"Multiple Methods found for signature. MethodName:{methodName}");
                return null;
            }
            return matchingMethods.SingleOrDefault();
        }

        public MethodSpec MatchMethodReference(MethodReference methodReference)
        {
            var parameterSpecs = _specManager.TryLoadParameterSpecs(() => methodReference.Parameters.ToArray(), null);
            _specManager.TryLoadTypeSpecs(() => methodReference.GenericParameters.ToArray(), out GenericParameterSpec[] genericTypeArgumentSpecs);
            var matchingMethods = Methods.Where(m
                    => m.Name == methodReference.Name
                    && m.Parameters.Length == methodReference.Parameters.Count
                    && m.HasExactParameters(parameterSpecs)
                    && m.HasExactGenericTypeArguments(genericTypeArgumentSpecs)
                    );
            if (matchingMethods.Count() > 1)
            {
                var methodArray = matchingMethods.ToArray();
                _specManager.AddFault(FaultSeverity.Error, $"Multiple Methods found for signature. MethodName:{methodReference.Name}");
                return null;
            }
            return matchingMethods.SingleOrDefault();
        }

        private void RegisterDependentTypeForModule(TypeSpec typeSpec)
        {
            if (Module == null)
            {
                _specManager.AddFault(FaultSeverity.Warning, $"Module not found for Type: {_typeDefinition}");
                return;
            }

            Module.RegisterDependentType(typeSpec);
        }
    }
}
