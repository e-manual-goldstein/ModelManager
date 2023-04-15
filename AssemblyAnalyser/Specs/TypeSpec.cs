using InvalidOperationException = System.InvalidOperationException;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using System;
using System.Reflection;
using AssemblyAnalyser.Specs;
using System.Collections.Concurrent;
using System.IO;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec//, IHasGenericParameters
    {
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
            IsSystem = Module?.IsSystem ?? true;
            IsClass = typeDefinition.IsClass;
        }
                
        protected TypeSpec(string fullTypeName, string uniqueTypeName, ISpecManager specManager) 
            : base(specManager)
        {
            UniqueTypeName = uniqueTypeName;
            FullTypeName = fullTypeName;
        }

        #region Properties

        public TypeDefinition Definition => _typeDefinition;
        public bool HasDefinition => _typeDefinition != null;

        public string UniqueTypeName { get; }
        public string FullTypeName { get; }
        public string Namespace { get; set; }
        public virtual bool IsInterface => _typeDefinition.IsInterface;

        public bool IsClass { get; }
        public bool IsArray { get; }

        #endregion

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
            IsCompilerGenerated = _typeDefinition?.CustomAttributes
                .Where(d => d.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName).Any() ?? false;
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
            var typeSpec = _specManager.LoadTypeSpec(_typeDefinition.BaseType);
            if (!typeSpec.IsNullSpec)
            {
                typeSpec.AddSubType(this);
            }            
            return typeSpec;
        }

        TypeSpec[] _interfaces;
        public TypeSpec[] Interfaces => _interfaces ??= CreateInterfaceSpecs();

        protected virtual TypeSpec[] CreateInterfaceSpecs()
        {
            var specs = _specManager.LoadTypeSpecs(_typeDefinition.Interfaces.Select(i => i.InterfaceType)).ToArray();
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
            return specs;
        }

        TypeSpec[] _nestedTypes;
        public TypeSpec[] NestedTypes => _nestedTypes ??= CreateNestedTypeSpecs();

        protected virtual TypeSpec[] CreateNestedTypeSpecs()
        {
            var specs = _specManager.LoadTypeSpecs(_typeDefinition.NestedTypes.Where(n => n.DeclaringType == _typeDefinition)).ToArray();
            foreach (var nestedType in specs.Where(s => !s.IsNullSpec))
            {
                nestedType.SetNestedIn(this);
                //nestedType.Process();
            }
            return specs;
        }

        MethodSpec[] _methods;
        public MethodSpec[] Methods => _methods ??= CreateMethodSpecs();

        protected virtual MethodSpec[] CreateMethodSpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault(FaultSeverity.Error, $"Unable to determine MethodSpecs for {this}");
                return Array.Empty<MethodSpec>();
            }
            var specs = TryLoadMethodSpecs(() => _typeDefinition.Methods.Where(m => m.DeclaringType == _typeDefinition).ToArray());
            return specs;
        }

        PropertySpec[] _properties;
        public PropertySpec[] Properties => _properties ??= CreatePropertySpecs();

        public virtual PropertySpec[] GetAllPropertySpecs()
        {
            return Properties.Union(BaseSpec.GetAllPropertySpecs()).ToArray();
        }

        protected virtual PropertySpec[] CreatePropertySpecs()
        {
            if (_typeDefinition == null)
            {
                _specManager.AddFault(FaultSeverity.Error, $"Unable to determine PropertySpecs for {this}");
                return Array.Empty<PropertySpec>();
            }
            var specs = TryLoadPropertySpecs(() => _typeDefinition.Properties.ToArray());
            return specs;
        }

        public virtual PropertySpec GetPropertySpec(string name, bool includeInherited = false)
        {
            var declaredProperties = Properties.Where(p => !p.Parameters.Any() && p.Name == name);
            if (!declaredProperties.Any() && includeInherited)
            {
                return BaseSpec.GetPropertySpec(name, includeInherited);
            }
            if (declaredProperties.Count() > 1)
            {
                return null;
            }
            return declaredProperties.SingleOrDefault();
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
            return _specManager
                .LoadTypeSpecs<GenericParameterSpec>(_typeDefinition.GenericParameters).ToArray();
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

        protected virtual void ProcessInterfaceImplementations()
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
            foreach (var interfaceMethod in interfaceSpec.Methods)
            {
                if (!MatchMethodByOverride(interfaceMethod))
                {
                    var methodSpec = MatchMethodSpecByNameAndParameterType(interfaceMethod.Name, interfaceMethod.Parameters,
                        interfaceMethod.GenericTypeParameters);
                    if (methodSpec == null)
                    {
                        _specManager.AddFault(FaultSeverity.Error, $"{this} does not implement {interfaceMethod}");
                    }
                    else
                    {
                        methodSpec.RegisterAsImplementation(interfaceMethod);
                    }
                }
            }
            foreach (var interfaceProperty in interfaceSpec.Properties)
            {
                if (!MatchPropertyByOverride(interfaceProperty))
                {
                    var propertySpec = GetPropertySpec(interfaceProperty.Name, true);
                    if (propertySpec == null)
                    {
                        _specManager.AddFault(FaultSeverity.Error, $"{this} does not implement {interfaceProperty}");
                    }
                    else
                    {
                        propertySpec.RegisterAsImplementation(interfaceProperty);
                    }
                }
            }
        }

        //private bool MatchPropertyByOverride(PropertySpec property)
        //{
        //    var overrides = GetAllPropertySpecs().Where(f => f.Overrides.Any()).ToDictionary(f => f, g => g.Overrides);
        //    var methodOverride = GetAllMethodSpecs().SingleOrDefault(m => m.Overrides.Contains(interfaceMethod));
        //    if (methodOverride != null)
        //    {
        //        methodOverride.RegisterAsImplementation(interfaceMethod);
        //        return true;
        //    }
        //    return false;
        //}

        private bool MatchMethodByOverride(MethodSpec interfaceMethod)
        {
            var overrides = GetAllMethodSpecs().Where(f => f.Overrides.Any()).ToDictionary(f => f, g => g.Overrides);
            var methodOverride = GetAllMethodSpecs().SingleOrDefault(m => m.Overrides.Contains(interfaceMethod));
            if (methodOverride != null)
            {
                methodOverride.RegisterAsImplementation(interfaceMethod);
                return true;
            }
            return false;
        }

        private bool MatchPropertyByOverride(PropertySpec property)
        {
            var overrides = GetAllPropertySpecs().Where(f => f.Overrides.Any()).ToDictionary(f => f, g => g.Overrides);
            var methodOverride = GetAllPropertySpecs().SingleOrDefault(m => m.Overrides.Contains(property));
            if (methodOverride != null)
            {
                methodOverride.RegisterAsImplementation(property);
                return true;
            }
            return false;
        }

        public virtual bool IsNullSpec => false;

        public bool IsErrorSpec { get; private set; }
        public bool IsCompilerGenerated { get; private set; }

        #endregion

        #region Method Specs

        ConcurrentDictionary<string, MethodSpec> _methodSpecs = new ConcurrentDictionary<string, MethodSpec>();

        public MethodSpec LoadMethodSpec(MethodReference method)
        {
            var methodDefinition = method as MethodDefinition;
            if (methodDefinition == null)
            {
                return new MissingMethodSpec(method, _specManager);
            }
            return _methodSpecs.GetOrAdd(methodDefinition.FullName, (key) => CreateMethodSpec(methodDefinition));
        }

        public MethodSpec LoadMethodSpec(MethodDefinition method)
        {
            return _methodSpecs.GetOrAdd(method.FullName, (key) => CreateMethodSpec(method));
        }

        private MethodSpec CreateMethodSpec(MethodDefinition method)
        {
            var spec = new MethodSpec(method, _specManager);            
            return spec;
        }

        public MethodSpec[] LoadMethodSpecs(MethodDefinition[] methodDefinitions)
        {
            return methodDefinitions.Select(m => LoadMethodSpec(m)).ToArray();
        }

        public MethodSpec[] TryLoadMethodSpecs(Func<MethodDefinition[]> getMethods)
        {
            MethodDefinition[] methods = null;
            try
            {
                methods = getMethods();
            }
            catch (TypeLoadException ex)
            {
                _specManager.AddFault(this, FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _specManager.AddFault(this, FaultSeverity.Error, ex.Message);
            }
            finally
            {
                methods ??= Array.Empty<MethodDefinition>();
            }
            return LoadMethodSpecs(methods);
        }

        public MethodSpec[] LoadSpecsForMethodReferences(MethodReference[] methodReferences)
        {
            return TryLoadMethodSpecs(() => methodReferences.Select(m => m.Resolve()).ToArray());
        }

        #endregion

        #region Property Specs

        public IReadOnlyDictionary<string, PropertySpec> PropertySpecs => _propertySpecs;

        ConcurrentDictionary<string, PropertySpec> _propertySpecs = new ConcurrentDictionary<string, PropertySpec>();

        public PropertySpec LoadPropertySpec(PropertyDefinition propertyDefinition)
        {
            return _propertySpecs.GetOrAdd(propertyDefinition.FullName, (def) => CreatePropertySpec(propertyDefinition));            
        }

        private PropertySpec CreatePropertySpec(PropertyDefinition propertyInfo)
        {
            return new PropertySpec(propertyInfo, _specManager);
        }

        public PropertySpec[] LoadPropertySpecs(PropertyDefinition[] propertyInfos)
        {
            return propertyInfos.Select(p => LoadPropertySpec(p)).ToArray();
        }

        public PropertySpec[] TryLoadPropertySpecs(Func<PropertyDefinition[]> getProperties)
        {
            PropertyDefinition[] properties = null;
            try
            {
                properties = getProperties();
            }
            catch (TypeLoadException ex)
            {
                _specManager.AddFault(this, FaultSeverity.Error, ex.Message);
            }            
            finally
            {
                properties ??= Array.Empty<PropertyDefinition>();
            }
            return LoadPropertySpecs(properties);
        }

        #endregion

        public override string ToString()
        {
            return 
                //$"{_typeDefinition.Namespace}_{_typeDefinition.FullName}" ?? 
                UniqueTypeName;
        }

        #region Post Build

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

        List<EventSpec> _delegateFor = new List<EventSpec>();
        public EventSpec[] DelegateForSpecs => _delegateFor.ToArray();

        public virtual void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            if (!_delegateFor.Contains(eventSpec))
            {
                _delegateFor.Add(eventSpec);
                RegisterDependentTypeForModule(eventSpec.DeclaringType);
            }
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

        #endregion

        public virtual bool MatchesSpec(TypeSpec typeSpec)
        {
            return Equals(typeSpec);
        }

        public MethodSpec GetMethodSpec(MethodReference method)
        {
            var matchingMethods = Methods.Where(m => m.IsSpecFor(method)).ToList();
            if (matchingMethods.Count > 1)
            {

            }
            return Methods.SingleOrDefault(m => m.IsSpecFor(method));
        }

        public MethodSpec[] GetMethodSpecs(string methodName, bool includeInherited = false)
        {
            return (includeInherited ? GetAllMethodSpecs() : Methods).Where(m => m.Name == methodName).ToArray();
        }

        public virtual MethodSpec[] GetAllMethodSpecs()
        {
            return Methods.Union(BaseSpec.GetAllMethodSpecs()).ToArray();
        }

        public MethodSpec MatchMethodSpecByNameAndParameterType(string methodName, ParameterSpec[] parameterSpecs
            , GenericParameterSpec[] genericTypeArgumentSpecs)
        {
            var nameAndParameterCountMatches = GetAllMethodSpecs().Where(m
                    => m.Name == methodName
                    && m.Parameters.Length == parameterSpecs.Length).ToArray();
            var matchingMethods = nameAndParameterCountMatches.Where(m
                    => m.HasExactGenericTypeParameters(genericTypeArgumentSpecs)
                    && m.HasExactParameters(parameterSpecs)
                    ).ToArray();
            if (matchingMethods.Count() > 1)
            {
                _specManager.AddFault(FaultSeverity.Error, $"Multiple Methods found for signature. MethodName:{methodName}");
                return null;
            }
            return matchingMethods.SingleOrDefault();
        }

        public MethodSpec MatchMethodReference(MethodReference methodReference)
        {
            var parameterSpecs = _specManager.TryLoadParameterSpecs(() => methodReference.Parameters.ToArray(), null);
            var genericTypeArgumentSpecs = _specManager
                .LoadTypeSpecs<GenericParameterSpec>(methodReference.GenericParameters).ToArray();
            return MatchMethodSpecByNameAndParameterType(methodReference.Name, parameterSpecs, genericTypeArgumentSpecs);
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
