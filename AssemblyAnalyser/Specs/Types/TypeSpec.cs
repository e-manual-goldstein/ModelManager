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
using AssemblyAnalyser.Extensions;

namespace AssemblyAnalyser
{
    public class TypeSpec : AbstractSpec//, IHasGenericParameters
    {
        TypeDefinition _typeDefinition;
        
        public TypeSpec(TypeDefinition typeDefinition, ModuleSpec moduleSpec, ISpecManager specManager)
            : this($"{typeDefinition.Namespace}.{typeDefinition.Name}", typeDefinition.FullName, moduleSpec, specManager)
        {
            _typeDefinition = typeDefinition;
            Name = typeDefinition.Name;
            Namespace = typeDefinition.Namespace;
            IsClass = typeDefinition.IsClass;
        }
                
        protected TypeSpec(string fullTypeName, string uniqueTypeName, ModuleSpec moduleSpec, ISpecManager specManager) 
            : base(specManager)
        {
            _module = moduleSpec;
            if (uniqueTypeName == "System.Action")
            {

            }
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

        public override bool IsSystem => Module.IsSystem;

        public bool IsClass { get; }

        public bool IsArray { get; }

        public virtual bool IsNullSpec => false;

        public virtual bool IsMissingSpec { get; }

        public bool IsCompilerGenerated { get; private set; }

        
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
                _specManager.AddFault(this, FaultSeverity.Error, "Cannot build Spec with null FullTypeName");
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
            CheckInterfaceImplementations();
            ProcessCompilerGenerated();
            ProcessGenerics();
        }

        protected virtual TypeSpec[] CreateAttributSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, Module.AssemblyLocator);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _typeDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, Module.AssemblyLocator);
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
        public ModuleSpec Module => _module;

        TypeSpec _baseSpec;
        public TypeSpec BaseSpec => _baseSpec ??= CreateBaseSpec();

        protected virtual TypeSpec CreateBaseSpec()
        {
            var typeSpec = _specManager.LoadTypeSpec(_typeDefinition.BaseType, Module.AssemblyLocator);
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
            var specs = _specManager.LoadTypeSpecs(_typeDefinition.Interfaces.Select(i => i.InterfaceType), Module.AssemblyLocator).ToArray();
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
            var specs = _specManager.LoadTypeSpecs(_typeDefinition.NestedTypes.Where(n => n.DeclaringType == _typeDefinition), Module.AssemblyLocator).ToArray();
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
                _specManager.AddFault(this, FaultSeverity.Error, $"Unable to determine MethodSpecs for {this}");
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
                _specManager.AddFault(this, FaultSeverity.Error, $"Unable to determine PropertySpecs for {this}");
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

        public virtual PropertySpec MatchPropertySpecByNameAndParameterType(string name, ParameterSpec[] parameterSpecs, bool includeInherited = false)
        {
            var matchingProperties = Properties.Where(p => 
                    p.Name == name
                    && p.Parameters.Length == parameterSpecs.Length
                    && p.HasExactParameters(parameterSpecs));
            if (!matchingProperties.Any() && includeInherited) 
            {
                return BaseSpec.MatchPropertySpecByNameAndParameterType(name, parameterSpecs, includeInherited);
            }
            if (matchingProperties.Count() > 1)
            {
                var methodArray = matchingProperties.ToArray();
                _specManager.AddFault(this, FaultSeverity.Error, $"Multiple Properties found for signature. PropertyName:{name}");
                return null;
            }
            return matchingProperties.SingleOrDefault();            
        }

        FieldSpec[] _fields;
        public FieldSpec[] Fields => _fields ??= CreateFieldSpecs();

        protected virtual FieldSpec[] CreateFieldSpecs()
        {
            var specs = TryLoadFieldSpecs(() => _typeDefinition.Fields.ToArray());
            return specs;
        }

        EventSpec[] _events;
        public EventSpec[] Events => _events ??= CreateEventSpecs();

        protected virtual EventSpec[] CreateEventSpecs()
        {
            var specs = TryLoadEventSpecs(() => _typeDefinition.Events.ToArray());
            return specs;
        }

        GenericParameterSpec[] _genericTypeParamters;
        public GenericParameterSpec[] GenericTypeParameters => _genericTypeParamters ??= CreateGenericTypeParameters();

        protected virtual GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return _specManager
                .LoadTypeSpecs<GenericParameterSpec>(_typeDefinition.GenericParameters, Module.AssemblyLocator).ToArray();
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

        protected virtual void CheckInterfaceImplementations()
        {
            foreach (var @interface in Interfaces.Where(i => !i.IsGenericInstance))
            {
                foreach (var interfaceMember in @interface.Properties)
                {
                    if (!interfaceMember.ImplementationFor.Intersect(Properties).Any())
                    {

                    }
                }
                foreach (var interfaceMember in @interface.Methods)
                {
                    if (!interfaceMember.ImplementationFor.Intersect(Methods).Any())
                    {

                    }
                }
            }
        }

        //[Obsolete]
        //private void RegisterMemberImplementations(TypeSpec interfaceSpec)
        //{
        //    //foreach (var interfaceMethod in interfaceSpec.Methods)
        //    //{
        //    //    if (!MatchMethodByOverride(interfaceMethod))
        //    //    {
        //    //        var methodSpec = FindMatchingMethodSpec(interfaceMethod, interfaceMethod);
        //    //        if (methodSpec == null)
        //    //        {
        //    //            _specManager.AddFault(this, FaultSeverity.Error, $"Missing Implementation: {interfaceMethod}");
        //    //        }
        //    //        else
        //    //        {
        //    //            methodSpec.RegisterAsImplementation(interfaceMethod);
        //    //        }
        //    //    }
        //    //}
        //    //foreach (var interfaceProperty in interfaceSpec.Properties)
        //    //{
        //    //    if (!MatchBySpecialNameMethods(interfaceProperty))
        //    //    {
        //    //        if (!MatchPropertyByOverride(interfaceProperty))
        //    //        {
        //    //            var propertySpec = GetPropertySpec(interfaceProperty.ExplicitName, true) ?? GetPropertySpec(interfaceProperty.Name, true);
        //    //            if (propertySpec == null)
        //    //            {
        //    //                _specManager.AddFault(this, FaultSeverity.Error, $"Missing Implementation {interfaceProperty}");
        //    //            }
        //    //            else
        //    //            {
        //    //                propertySpec.RegisterAsImplementation(interfaceProperty);
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //}

        //[Obsolete]
        //protected virtual bool MatchBySpecialNameMethods(PropertySpec interfaceProperty)
        //{
        //    var specialNameMethods = Methods.Where(m => m.IsSpecialName).ToArray();
        //    var implementers = specialNameMethods.Where(m 
        //        => m.ImplementationFor.Contains(interfaceProperty.Getter)
        //        || m.ImplementationFor.Contains(interfaceProperty.Setter))
        //        .ToArray();
        //    var backedProperties = implementers.Select(m => m.SpecialNameMethodForMember).Where(t => t != null).Distinct().ToArray();
        //    if (!backedProperties.Any())
        //    {
        //        return BaseSpec.MatchBySpecialNameMethods(interfaceProperty);
        //    }
        //    if (backedProperties.Count() > 1)
        //    {
        //        _specManager.AddFault(this, FaultSeverity.Error, "Multiple backed Properties found");
        //        return false;
        //    }
        //    var backedProperty = backedProperties.Single();
        //    //var matchingGetter = specialNameMethods
        //    //    .Where(m => m.Implements == interfaceProperty.Getter)
        //    //    .SingleOrDefault();
        //    //var matchingSetter = specialNameMethods
        //    //    .Where(m => m.Implements == interfaceProperty.Getter)
        //    //    .SingleOrDefault();
        //    //if (matchingGetter.SpecialNameMethodForMember != matchingSetter.SpecialNameMethodForMember)
        //    //{
        //    //    _specManager.AddFault(this, FaultSeverity.Error, "Unexpected mismatch of special name methods");
        //    //}
        //    //else
        //    //{
        //        (backedProperty as PropertySpec).RegisterAsImplementation(interfaceProperty);
        //    //}
        //    return true;
        //}

        //[Obsolete]
        //public virtual bool MatchMethodByOverride(MethodSpec method)
        //{
        //    var methodOverrides = Methods.Where(m => m.Overrides.Contains(method));
        //    if (methodOverrides.Any())
        //    {
        //        if (methodOverrides.Count() > 1)
        //        {
        //            _specManager.AddFault(this, FaultSeverity.Critical, $"Multiple Methods found to override spec {method}");
        //            return true;
        //        }
        //        methodOverrides.Single().RegisterAsImplementation(method);
        //        return true;
        //    }
        //    return BaseSpec.MatchMethodByOverride(method);
        //}

        //[Obsolete]
        //public virtual bool MatchPropertyByOverride(PropertySpec property)
        //{
        //    var propertyOverrides = Properties.Where(m => m.Overrides.Contains(property));
        //    if (propertyOverrides.Any())
        //    {
        //        if (propertyOverrides.Count() > 1)
        //        {
        //            _specManager.AddFault(this, FaultSeverity.Critical, $"Multiple Methods found to override spec {property}");
        //            return true;
        //        }                
        //        propertyOverrides.Single().RegisterAsImplementation(property);
        //        return true;
        //    }
        //    return BaseSpec.MatchPropertyByOverride(property);
        //}

        #endregion

        #region Method Specs

        ConcurrentDictionary<string, MethodSpec> _methodSpecs = new ConcurrentDictionary<string, MethodSpec>();

        public virtual MethodSpec LoadMethodSpec(MethodReference method)
        {
            var methodDefinition = TryGetMethodDefinition(method);
            if (methodDefinition != null)
            {
                return _methodSpecs.GetOrAdd(methodDefinition.CreateUniqueMethodName(), (key) => CreateMethodSpec(methodDefinition));
            }
            return new MissingMethodSpec(method, this, _specManager);
        }

        public virtual MethodDefinition TryGetMethodDefinition(MethodReference method)
        {
            if (method is MethodDefinition methodDefinition)
            {
                return methodDefinition;
            }
            else if (IsSystem)
            {
                try
                {
                    //Should be able to safely resolve method for System Types
                    return method.Resolve();
                }
                catch
                {
                    _specManager.AddFault(this, FaultSeverity.Critical, $"Failed to resolve MethodDefinition for System Type. Method: {method.Name}");
                    return null;
                }
            }
            var methodsByName = Definition.Methods.Where(m => m.FullName == method.FullName).ToArray();
            if (methodsByName.Length > 1)
            {
                var methodsByParam = methodsByName.Where(p => p.HasExactParameters(method.Parameters.ToArray())).ToArray();
            }
            return methodsByName.SingleOrDefault();
        }

        public MethodSpec LoadMethodSpec(MethodDefinition method)
        {
            return _methodSpecs.GetOrAdd(method.CreateUniqueMethodName(), (key) => CreateMethodSpec(method));
        }

        private MethodSpec CreateMethodSpec(MethodDefinition method)
        {
            var spec = method.HasGenericParameters 
                ? new GenericMethodSpec(method, this, _specManager) 
                : new MethodSpec(method, this, _specManager);
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

        ConcurrentDictionary<string, PropertySpec> _propertySpecs = new ConcurrentDictionary<string, PropertySpec>();

        public PropertySpec LoadPropertySpec(PropertyDefinition propertyDefinition)
        {
            return _propertySpecs.GetOrAdd(propertyDefinition.FullName, (def) => CreatePropertySpec(propertyDefinition));            
        }

        private PropertySpec CreatePropertySpec(PropertyDefinition propertyInfo)
        {
            //TODO: Is it faster to flag the explicit implementations here?");
            var spec = new PropertySpec(propertyInfo, this, _specManager);
            //RegisterImplementations(spec);
            return spec;
        }

        //private void RegisterImplementations(PropertySpec spec)
        //{
        //    foreach (var @override in spec.Overrides)
        //    {
        //        if (@override.DeclaringType.IsInterface)
        //        {
        //            spec.RegisterAsImplementation(@override);
        //        }
        //        else
        //        {
        //            //Abastract Class Possibly?
        //        }
        //    }

        //    //foreach (var @interface in Interfaces.Where(i => !i.IsGenericInstance)) 
        //    //    {
        //    //        if (spec.Name.StartsWith(@interface.FullTypeName))
        //    //        {
        //    //            var trimmedPropertyName = spec.Name.Replace($"{@interface.FullTypeName}.", "");
        //    //            var matchingExplicitInterfaceProperties = @interface.Properties.Where(i => i.Name == trimmedPropertyName);
        //    //            if (matchingExplicitInterfaceProperties.Count() != 1)
        //    //            {
        //    //                _specManager.AddFault(this, FaultSeverity.Error, "Could not determine implementation");
        //    //            }
        //    //            else
        //    //            {
        //    //                spec.RegisterAsImplementation(matchingExplicitInterfaceProperties.Single());                            
        //    //            }
        //    //        }
        //    //    var matchingInterfaceProperties = @interface.Properties
        //    //        .Where(i => i.Name == spec.Name && i.HasExactParameters(spec.Parameters));
        //    //    if (!matchingInterfaceProperties.Any())
        //    //    {
        //    //        continue;
        //    //    }
        //    //    else if (matchingInterfaceProperties.Count() > 1)
        //    //    {

        //    //    }
        //    //    spec.RegisterAsImplementation(matchingInterfaceProperties.Single());
        //    //}
            
        //}



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

        #region Field Specs

        ConcurrentDictionary<string, FieldSpec> _fieldSpecs = new ConcurrentDictionary<string, FieldSpec>();

        private FieldSpec LoadFieldSpec(FieldDefinition fieldDefinition)
        {
            FieldSpec fieldSpec = _fieldSpecs.GetOrAdd(fieldDefinition.FullName, (spec) => CreateFieldSpec(fieldDefinition));
            return fieldSpec;
        }

        private FieldSpec CreateFieldSpec(FieldDefinition fieldInfo)
        {
            return new FieldSpec(fieldInfo, this, _specManager);
        }

        public FieldSpec[] LoadFieldSpecs(FieldDefinition[] fieldInfos)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f)).ToArray();
        }

        public FieldSpec[] TryLoadFieldSpecs(Func<FieldDefinition[]> getFields)
        {
            return LoadFieldSpecs(getFields());
        }


        #endregion

        #region Event Specs

        ConcurrentDictionary<string, EventSpec> _eventSpecs = new ConcurrentDictionary<string, EventSpec>();

        public EventSpec[] TryLoadEventSpecs(Func<EventDefinition[]> getEvents)
        {
            return LoadEventSpecs(getEvents());
        }

        private EventSpec LoadEventSpec(EventDefinition eventInfo)
        {
            EventSpec fieldSpec = _eventSpecs.GetOrAdd(eventInfo.FullName, (e) => CreateEventSpec(eventInfo));
            return fieldSpec;
        }

        private EventSpec CreateEventSpec(EventDefinition eventInfo)
        {
            return new EventSpec(eventInfo, this, _specManager);
        }

        public EventSpec[] LoadEventSpecs(EventDefinition[] eventInfos)
        {
            return eventInfos.Select(e => LoadEventSpec(e)).ToArray();
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
                    _specManager.AddFault(this, FaultSeverity.Error, $"NestedIn already set for Type {this}");
                }
                else
                {
                    _specManager.AddMessage($"NestedIn already set to this value: {typeSpec}");
                }
                return;
            }
            NestedIn = typeSpec;
        }

        protected List<TypeSpec> _implementations = new List<TypeSpec>();

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

        protected List<TypeSpec> _subTypes = new List<TypeSpec>();
        public TypeSpec[] GetSubTypes => _subTypes.ToArray();
        public virtual void AddSubType(TypeSpec typeSpec)
        {
            if (!_subTypes.Contains(typeSpec))
            {
                _subTypes.Add(typeSpec);
                RegisterDependentTypeForModule(typeSpec);
            }
        }

        protected ConcurrentDictionary<IMemberSpec, TypeSpec> _resultTypeLookup = new();
        protected List<IMemberSpec> _resultTypeSpecs = new List<IMemberSpec>();
        public IMemberSpec[] ResultTypeSpecs => _resultTypeSpecs.ToArray();
        
        public virtual void RegisterAsResultType(IMemberSpec methodSpec)
        {
            _resultTypeLookup.GetOrAdd(methodSpec, (spec) => spec.DeclaringType);
            //if (!_resultTypeSpecs.Contains(methodSpec))
            //{
            //    _resultTypeSpecs.Add(methodSpec);
            //    RegisterDependentTypeForModule(methodSpec.DeclaringType);
            //}
        }

        protected List<ParameterSpec> _dependentParameterSpecs = new List<ParameterSpec>();
        public ParameterSpec[] DependentParameterSpecs => _dependentParameterSpecs.ToArray();

        public virtual void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            //if (!_dependentParameterSpecs.Contains(parameterSpec))
            //{
                _dependentParameterSpecs.Add(parameterSpec);
                //RegisterDependentTypeForModule(parameterSpec.Member.DeclaringType);
            //}
        }

        protected List<MethodSpec> _dependentMethodBodies = new List<MethodSpec>();
        public MethodSpec[] DependentMethodBodies => _dependentMethodBodies.ToArray();

        public virtual void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            //if (!_dependentMethodBodies.Contains(methodSpec))
            //{
                _dependentMethodBodies.Add(methodSpec);
            //    RegisterDependentTypeForModule(methodSpec.DeclaringType);
            //}
        }

        List<AbstractSpec> _decoratorForSpecs = new List<AbstractSpec>();

        public AbstractSpec[] DecoratorForSpecs => _decoratorForSpecs.ToArray();

        public virtual void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            //if (!_decoratorForSpecs.Contains(decoratedSpec))
            //{
            //    _decoratorForSpecs.Add(decoratedSpec);
            //    //TODO Finish this part
            //    //Assembly.RegisterDependentType(decoratedSpec);
            //}
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

        public MethodSpec[] GetMethodSpecs(IHasExplicitName namedMethod, bool includeInherited = false)
        {
            var methods = includeInherited ? GetAllMethodSpecs() : Methods;
            var explicitMethods = methods.Where(m => m.Name == namedMethod.ExplicitName).ToArray();
            if (!explicitMethods.Any())
            {
                return methods.Where(m => m.Name == namedMethod.Name).ToArray();
            }
            return explicitMethods;
        }

        public virtual MethodSpec[] GetAllMethodSpecs()
        {
            return Methods.Union(BaseSpec.GetAllMethodSpecs()).ToArray();
        }

        public virtual MethodSpec FindMatchingMethodSpec(MethodSpec methodSpec)
        {
            var nameAndParameterCountMatches = GetMethodSpecs(methodSpec)
                .Where(m => m.Parameters.Length == methodSpec.Parameters.Length).ToArray();
            var matchingMethods = nameAndParameterCountMatches.Where(m => m.MatchesSpec(methodSpec)).ToArray();
            if (!matchingMethods.Any())
            {
                return BaseSpec.FindMatchingMethodSpec(methodSpec);
            }
            else if (matchingMethods.Count() > 1)
            {
                _specManager.AddFault(this, FaultSeverity.Error, $"Multiple Methods found for signature. MethodName:{methodSpec.ExplicitName}");
                return null;
            }
            return matchingMethods.SingleOrDefault();
        }

    }
}
