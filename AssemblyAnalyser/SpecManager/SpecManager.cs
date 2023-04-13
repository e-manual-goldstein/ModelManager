﻿using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssemblyAnalyser.Specs;
using AssemblyAnalyser.Faults;
using static System.Formats.Asn1.AsnWriter;

namespace AssemblyAnalyser
{
    public class SpecManager : ISpecManager
    {
        Dictionary<string, string> _workingFiles;
        readonly ILogger _logger;
        readonly IExceptionManager _exceptionManager;
        readonly DefaultAssemblyResolver _assemblyResolver;
        object _lock = new object();
        private bool _disposed;


        public SpecManager(ILoggerProvider loggerProvider, IExceptionManager exceptionManager)
        {            
            _logger = loggerProvider.CreateLogger("Spec Manager");
            _exceptionManager = exceptionManager;
            _assemblyResolver = CreateAssemblyResolver();

        }

        private DefaultAssemblyResolver CreateAssemblyResolver()
        {
            var resolver = new DefaultAssemblyResolver();            
            return resolver;
        }
                
        List<IRule> _specRules = new List<IRule>();
        public IRule[] SpecRules => _specRules.ToArray();
        
        List<BuildFault> _faults = new List<BuildFault>();
        public BuildFault[] Faults => _faults.ToArray();

        public void AddFault(string faultMessage)
        {
            LogFault(null, faultMessage);
            _faults.Add(new BuildFault(faultMessage));
        }

        public void AddFault(FaultSeverity faultSeverity, string faultMessage)
        {
            LogFault(faultSeverity, faultMessage);
            _faults.Add(new BuildFault(faultSeverity, faultMessage));
        }

        public void AddFault(ISpec specContext, FaultSeverity faultSeverity, string faultMessage)
        {
            LogFault(faultSeverity, faultMessage);
            _faults.Add(new BuildFault(specContext, faultSeverity, faultMessage));
        }

        private void LogFault(FaultSeverity? faultSeverity, string faultMessage)
        {
            switch (faultSeverity)
            {
                case FaultSeverity.Error:
                    _logger.Log(LogLevel.Error, faultMessage);
                    break;
                case FaultSeverity.Warning:
                    _logger.Log(LogLevel.Warning, faultMessage);
                    break;
                case FaultSeverity.Information:
                    _logger.Log(LogLevel.Information, faultMessage);
                    break;
                default:
                    _logger.Log(LogLevel.Debug, faultMessage);
                    break;
            }
        }

        List<string> _messages = new List<string>();
        public string[] Messages => _messages.ToArray();

        public void AddMessage(string msg)
        {
            Console.WriteLine(msg);
            _messages.Add(msg);
        }

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingFiles = Directory.EnumerateFiles(workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
        }

        public void Reset()
        {
            _moduleSpecs.Clear();
            //_methodSpecs.Clear();
            _parameterSpecs.Clear();
            _propertySpecs.Clear();
            _fieldSpecs.Clear();
        }

        public void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = true) where TSpec : AbstractSpec
        {
            if (!parallelProcessing)
            {
                foreach (var type in specs)
                {
                    type.Process();
                }
            }
            else
            {
                Parallel.ForEach(specs,
                    new ParallelOptions() { MaxDegreeOfParallelism = 16 }, (t) =>
                    {
                        t.Process();
                    });
            }
        }

        public void ProcessAll(bool includeSystem = true, bool parallelProcessing = true)
        {
            //ProcessAllAssemblies(includeSystem, parallelProcessing);
            //ProcessLoadedMethods(includeSystem, parallelProcessing);
            ProcessLoadedProperties(includeSystem);
            ProcessLoadedParameters(includeSystem);
            ProcessLoadedFields(includeSystem);
            ProcessLoadedEvents(includeSystem);
            ProcessLoadedAttributes(includeSystem);
        }

        #region Modules

        public IReadOnlyDictionary<string, ModuleSpec> Modules => _moduleSpecs;

        ConcurrentDictionary<string, ModuleSpec> _moduleSpecs = new ConcurrentDictionary<string, ModuleSpec>();

        public void ProcessAllModules(bool includeSystem = true, bool parallelProcessing = true)
        {
            var list = new List<ModuleSpec>();

            var assemblySpecs = Modules.Values;

            var nonSystemAssemblies = assemblySpecs.Where(a => includeSystem || !a.IsSystem).ToArray();
            
            list = nonSystemAssemblies.Where(a => includeSystem || !a.IsSystem).ToList();
            if (parallelProcessing)
            {
                Parallel.ForEach(list, l => l.Process());
            }
            else
            {
                foreach (var item in list)
                {
                    item.Process();
                }
            }
        }

        public ModuleSpec LoadModuleSpec(IMetadataScope scope)
        {
            return _moduleSpecs.GetOrAdd(scope.GetScopeNameWithoutExtension(), (key) => CreateFullModuleSpec(scope));
        }

        public ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new NotImplementedException();
            }
            if (typeReference.IsGenericInstance)
            {
                return _moduleSpecs.GetOrAdd(typeReference.Module.Name,
                    (key) => CreateFullModuleSpec(typeReference.Module));
            }
            if (typeReference.Resolve() is TypeDefinition typeDefinition)
            {
                _moduleSpecs.GetOrAdd(typeDefinition.Scope.GetScopeNameWithoutExtension(),
                    (key) => CreateFullModuleSpec(typeDefinition.Scope));
            }
            return _moduleSpecs.GetOrAdd(typeReference.Scope.GetScopeNameWithoutExtension(), 
                (key) => CreateFullModuleSpec(typeReference.Scope));
        }


        public ModuleSpec LoadModuleSpecFromPath(string moduleFilePath)
        {
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = _assemblyResolver
            };
            var moduleDefinition = ModuleDefinition.ReadModule(moduleFilePath, readerParameters);
            return LoadModuleSpec(moduleDefinition);
        }

        public ModuleSpec[] LoadReferencedModules(ModuleDefinition baseModule)
        {
            var specs = new List<ModuleSpec>();
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {                
                var moduleSpec = LoadReferencedModuleByFullName(baseModule, assemblyReference.FullName);
                if (moduleSpec != null)
                {
                    specs.Add(moduleSpec);
                }
            }
            return specs.OrderBy(s => s.FilePath).ToArray();
        }

        public ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, string referencedModuleName)
        {
            if (module.Name == referencedModuleName)
            {
                return LoadModuleSpec(module);
            }
            var locator = AssemblyLocator.GetLocator(module);
            var assemblyReference = module.AssemblyReferences.Single(a => a.FullName.Contains(referencedModuleName));
            return LoadReferencedModule(locator, assemblyReference);
        }

        public ModuleSpec LoadReferencedModuleByScopeName(ModuleDefinition module, IMetadataScope scope)
        {
            if (module.GetScopeNameWithoutExtension() == scope.GetScopeNameWithoutExtension())
            {
                return LoadModuleSpec(module);
            }
            var locator = AssemblyLocator.GetLocator(module);
            var version = scope switch
            {
                AssemblyNameReference assemblyNameReference => assemblyNameReference.Version,
                ModuleDefinition moduleDefinition => moduleDefinition.Assembly.Name.Version,
                _ => throw new NotImplementedException()
            };
            var assemblyReference = module.AssemblyReferences
                .Single(a => a.FullName.ParseShortName() == scope.GetScopeNameWithoutExtension() && a.Version == version);
            return LoadReferencedModule(locator, assemblyReference);            
        }

        private ModuleSpec LoadReferencedModule(AssemblyLocator locator, AssemblyNameReference assemblyReference)
        {
            try
            {
                var assemblyLocation = locator.LocateAssemblyByName(assemblyReference.FullName);
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    var missingModuleSpec = _moduleSpecs.GetOrAdd(assemblyReference.Name, (key) => CreateMissingModuleSpec(assemblyReference));
                    missingModuleSpec.AddModuleVersion(assemblyReference);
                    return missingModuleSpec;
                }
                var moduleSpec = LoadModuleSpecFromPath(assemblyLocation);
                moduleSpec.AddModuleVersion(assemblyReference);
                return moduleSpec;

            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                AddFault(FaultSeverity.Error, $"Unable to load assembly {assemblyReference.FullName}. Required by {assemblyReference}");
            }
            return null;
        }

        public ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] types)
        {
            return types.Select(t => LoadModuleSpec(t)).ToArray();
        }

        private ModuleSpec CreateFullModuleSpec(IMetadataScope scope)
        {
            try
            {
                return scope switch
                {
                    AssemblyNameDefinition assemblyNameDefinition => new MissingModuleSpec(assemblyNameDefinition, this),
                    ModuleDefinition moduleDefinition => new ModuleSpec(moduleDefinition, moduleDefinition.FileName, this),
                    AssemblyNameReference assemblyNameReference => LoadModuleByAssemblyNameReference(assemblyNameReference),
                    _ => throw new NotImplementedException()
                };                
            }
            catch
            {

            }
            return new MissingModuleSpec(scope as AssemblyNameReference, this);
        }

        private ModuleSpec LoadModuleByAssemblyNameReference(AssemblyNameReference assemblyNameReference)
        {
            return new MissingModuleSpec(assemblyNameReference, this);
        }

        private ModuleSpec CreateMissingModuleSpec(AssemblyNameReference assemblyNameReference)
        {
            var spec = new MissingModuleSpec(assemblyNameReference, this);
            return spec;
        }

        #endregion

        #region Types

        static NullTypeSpec _nullTypeSpec;

        public TypeSpec GetNullTypeSpec()
        {
            return _nullTypeSpec ??= new NullTypeSpec(this);
        }

        public TypeSpec[] TypeSpecs => Modules.Values.SelectMany(m => m.TypeSpecs).ToArray();

        public TypeSpec LoadTypeSpec(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                return GetNullTypeSpec();
            }
            ModuleSpec module = LoadModuleSpecForTypeReference(typeReference);
            return module.LoadTypeSpec(typeReference);
        }

        public IEnumerable<TypeSpec> LoadTypeSpecs(IEnumerable<TypeReference> types)
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference);
            }
        }

        public IEnumerable<TSpec> LoadTypeSpecs<TSpec>(IEnumerable<TypeReference> types) 
            where TSpec : TypeSpec
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference) as TSpec;
            }
        }

        #endregion

        #region Method Specs

        public MethodSpec[] MethodSpecs => TypeSpecs.SelectMany(t => t.Methods).ToArray();

        public MethodSpec LoadMethodSpec(MethodDefinition method)
        {
            if (method == null)
            {
                AddFault(FaultSeverity.Information, "No MethodSpec for null MethodDefintion");
                return null;
            }
            return LoadTypeSpec(method.DeclaringType).LoadMethodSpec(method);
        }

        public MethodSpec[] LoadMethodSpecs(MethodDefinition[] methodDefinitions)
        {
            if (methodDefinitions.Any(m => m == null))
            {

            }
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
                AddFault(FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                AddFault(FaultSeverity.Error, ex.Message);
            }
            finally
            {
                methods ??= Array.Empty<MethodDefinition>();
            }
            return LoadMethodSpecs(methods);
        }

        public MethodSpec[] LoadSpecsForMethodReferences(MethodReference[] methodReferences)
        {
            var definitions = methodReferences.Select(m => m.Resolve()).ToArray();
            if (definitions.Any(d => d == null))
            {

            }
            return TryLoadMethodSpecs(() => methodReferences.Select(m => m.Resolve()).ToArray());
        }

        #endregion

        #region Property Specs

        public IReadOnlyDictionary<PropertyDefinition, PropertySpec> Properties => _propertySpecs;

        ConcurrentDictionary<PropertyDefinition, PropertySpec> _propertySpecs = new ConcurrentDictionary<PropertyDefinition, PropertySpec>();

        public void ProcessLoadedProperties(bool includeSystem = true)
        {
            foreach (var (propertyName, prop) in Properties.Where(t => includeSystem || !t.Value.IsSystem))
            {
                prop.Process();
            }
        }

        private PropertySpec LoadPropertySpec(PropertyDefinition propertyDefinition)
        {
            PropertySpec propertySpec = _propertySpecs.GetOrAdd(propertyDefinition, (def) => CreatePropertySpec(def));
            return propertySpec;
        }

        private PropertySpec CreatePropertySpec(PropertyDefinition propertyInfo)
        {
            return new PropertySpec(propertyInfo, this);
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
                AddFault(FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                properties ??= Array.Empty<PropertyDefinition>();
            }
            return LoadPropertySpecs(properties);
        }

        #endregion

        #region Parameter Specs

        public IReadOnlyDictionary<ParameterDefinition, ParameterSpec> Parameters => _parameterSpecs;

        ConcurrentDictionary<ParameterDefinition, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<ParameterDefinition, ParameterSpec>();

        public void ProcessLoadedParameters(bool includeSystem = true)
        {
            foreach (var (parameterName, param) in Parameters.Where(t => includeSystem || t.Value.IsSystemParameter.Equals(false)))
            {
                param.Process();
            }
        }

        private ParameterSpec LoadParameterSpec(ParameterDefinition parameterDefinition, IMemberSpec member)
        {
            return _parameterSpecs.GetOrAdd(parameterDefinition, CreateParameterSpec(parameterDefinition, member));
        }

        private ParameterSpec CreateParameterSpec(ParameterDefinition parameterDefinition, IMemberSpec member)
        {
            //var typeSpecs = LoadTypeSpecs(parameterDefinition.CustomAttributes.Select(t => t.AttributeType));
            return new ParameterSpec(parameterDefinition, member, this);
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterDefinition[] parameterDefinitions, IMemberSpec member)
        {
            return parameterDefinitions?.Select(p => LoadParameterSpec(p, member)).ToArray();
        }

        public ParameterSpec[] TryLoadParameterSpecs(Func<ParameterDefinition[]> parameterDefinitions, IMemberSpec member)
        {
            ParameterDefinition[] parameterInfos = null;
            try
            {
                parameterInfos = parameterDefinitions();
            }
            catch (TypeLoadException typeLoadException)
            {

            }
            catch (Exception)
            {

            }
            return LoadParameterSpecs(parameterInfos, member);
        }

        #endregion

        #region Field Specs

        public IReadOnlyDictionary<FieldDefinition, FieldSpec> Fields => _fieldSpecs;

        ConcurrentDictionary<FieldDefinition, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldDefinition, FieldSpec>();

        public void ProcessLoadedFields(bool includeSystem = true)
        {
            foreach (var (fieldName, field) in Fields.Where(t => includeSystem || !t.Value.IsSystem))
            {
                field.Process();
            }
        }

        private FieldSpec LoadFieldSpec(FieldDefinition fieldInfo, TypeSpec declaringType)
        {
            FieldSpec fieldSpec = _fieldSpecs.GetOrAdd(fieldInfo, (spec) => CreateFieldSpec(fieldInfo, declaringType));
            return fieldSpec;
        }

        private FieldSpec CreateFieldSpec(FieldDefinition fieldInfo, TypeSpec declaringType)
        {
            return new FieldSpec(fieldInfo, declaringType, this);
        }

        public FieldSpec[] LoadFieldSpecs(FieldDefinition[] fieldInfos, TypeSpec declaringType)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f, declaringType)).ToArray();
        }

        public FieldSpec[] TryLoadFieldSpecs(Func<FieldDefinition[]> getFields, TypeSpec declaringType)
        {
            FieldDefinition[] fields = null;
            try
            {
                fields = getFields();
            }
            catch (TypeLoadException ex)
            {
                AddFault(FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                fields ??= Array.Empty<FieldDefinition>();
            }
            return LoadFieldSpecs(fields, declaringType);
        }

        #endregion

        #region Attribute Specs

        public IReadOnlyDictionary<string, TypeSpec> Attributes => _attributeSpecs;

        ConcurrentDictionary<string, TypeSpec> _attributeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttribute[]> getAttributes, AbstractSpec decoratedSpec)
        {
            CustomAttribute[] attributes = null;
            try
            {
                attributes = getAttributes();
            }
            catch (TypeLoadException ex)
            {
                AddFault(FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                attributes ??= Array.Empty<CustomAttribute>();
            }
            return LoadAttributeSpecs(attributes, decoratedSpec);
        }

        public TypeSpec[] LoadAttributeSpecs(CustomAttribute[] attibutes, AbstractSpec decoratedSpec)
        {
            return attibutes.Select(f => LoadAttributeSpec(f, decoratedSpec)).ToArray();
        }

        private TypeSpec LoadAttributeSpec(CustomAttribute attribute, AbstractSpec decoratedSpec)
        {
            var attributeSpec = LoadTypeSpec(attribute.AttributeType);
            _attributeSpecs.GetOrAdd(attributeSpec.FullTypeName, attributeSpec);
            attributeSpec.RegisterAsDecorator(decoratedSpec);
            return attributeSpec;
        }

        public void ProcessLoadedAttributes(bool includeSystem = true)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Event Specs

        public IReadOnlyDictionary<EventDefinition, EventSpec> Events => _eventSpecs;

        ConcurrentDictionary<EventDefinition, EventSpec> _eventSpecs = new ConcurrentDictionary<EventDefinition, EventSpec>();

        public void ProcessLoadedEvents(bool includeSystem = true)
        {
            foreach (var (fieldName, field) in Events.Where(t => includeSystem || t.Value.IsSystemEvent.Equals(false)))
            {
                field.Process();
            }
        }

        public EventSpec[] TryLoadEventSpecs(Func<EventDefinition[]> getEvents, TypeSpec declaringType)
        {
            EventDefinition[] events = null;
            try
            {
                events = getEvents();
            }
            catch (TypeLoadException ex)
            {
                AddFault(FaultSeverity.Error, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                events ??= Array.Empty<EventDefinition>();
            }
            return LoadEventSpecs(events, declaringType);
        }

        private EventSpec LoadEventSpec(EventDefinition eventInfo, TypeSpec declaringType)
        {
            EventSpec fieldSpec = _eventSpecs.GetOrAdd(eventInfo, (e) => CreateEventSpec(eventInfo, declaringType));
            return fieldSpec;
        }

        private EventSpec CreateEventSpec(EventDefinition eventInfo, TypeSpec declaringType)
        {
            return new EventSpec(eventInfo, declaringType, this);
        }

        public EventSpec[] LoadEventSpecs(EventDefinition[] eventInfos, TypeSpec declaringType)
        {
            return eventInfos.Select(e => LoadEventSpec(e, declaringType)).ToArray();
        }

        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ISpecDependency RegisterOperandDependency(object operand, MethodSpec methodSpec)
        {
            if (operand is MethodReference methodRef)
            {
                //kludge
                if (methodRef.DeclaringType.IsGenericInstance)
                {
                    return null;
                }
            }
            return operand switch
            {
                TypeReference typeReference => new MethodToTypeDependency(methodSpec, LoadTypeSpec(typeReference)),
                //MethodReference methodReference => new MethodToMethodDependency(methodSpec, LoadMethodSpec(methodReference)),
                //FieldReference fieldReference => fieldReference.Module,
                //ParameterReference parameterReference => parameterReference.ParameterType.Module,
                //VariableReference variableReference => variableReference.VariableType.Module,
                //Instruction operandInstruction => operandInstruction.Operand,
                //Instruction[] operandInstructions => operandInstructions.Select(t => t.Operand),
                _ => null
            };
            
        }

        

        #endregion

    }
}
