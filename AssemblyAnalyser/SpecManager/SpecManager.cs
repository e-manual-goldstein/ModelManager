using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssemblyAnalyser.Specs;

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
        
        List<string> _faults = new List<string>();
        public string[] Faults => _faults.ToArray();

        public void AddFault(string faultMessage)
        {
            _faults.Add(faultMessage);
        }

        public void AddFault(FaultSeverity faultSeverity, string faultMessage)
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
            AddFault($"[{faultSeverity}]\t{faultMessage}");
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
            _methodSpecs.Clear();
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
            ProcessLoadedMethods(includeSystem, parallelProcessing);
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

        public ModuleSpec LoadModuleSpec(ModuleDefinition module)
        {
            if (module == null)
            {
                throw new NotImplementedException();
            }
            return _moduleSpecs.GetOrAdd(module.Assembly.Name.Name, (key) => CreateFullModuleSpec(module));
        } 
        
        public ModuleSpec LoadModuleSpec(string moduleFilePath)
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
                var moduleSpec = LoadModuleSpec(assemblyLocation);
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

        private ModuleSpec CreateFullModuleSpec(ModuleDefinition module)
        {
            var spec = new ModuleSpec(module, module.FileName, this);
            return spec;
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

        public TypeSpec[] ProcessedTypes => TypeSpecs.Where(t => t.IsProcessed).OrderBy(s => s.FullTypeName).ToArray();

        public TypeSpec[] TypeSpecs => Modules.Values.SelectMany(m => m.TypeSpecs).ToArray();

        public bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec)
        {
            bool success = false;
            try
            {
                var type = getType();
                if (type != null)
                {
                    ModuleSpec module = LoadModuleSpec(type.Module);
                    module.TryLoadTypeSpec(getType, out typeSpec);
                }
                else
                {
                    typeSpec = GetNullTypeSpec();
                }
                success = true;
            }
            catch (TypeLoadException ex)
            {
                if (!string.IsNullOrEmpty(ex.TypeName))
                {
                    throw new NotImplementedException();
                }
                typeSpec = TypeSpec.CreateErrorSpec($"{ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
                typeSpec = TypeSpec.CreateErrorSpec($"{ex.Message}");
            }
            catch (Exception ex)
            {
                typeSpec = TypeSpec.CreateErrorSpec($"{ex.Message}");
            }
            return success;
        }

        public TypeSpec LoadTypeSpec(TypeReference typeReference)
        {
            ModuleSpec module = LoadModuleSpec(typeReference.Module);
            return module.LoadTypeSpec(typeReference);
        }

        public bool TryLoadTypeSpecs(Func<TypeReference[]> getTypes, out TypeSpec[] typeSpecs)
        {
            typeSpecs = Array.Empty<TypeSpec>();
            bool success = false;
            try
            {
                typeSpecs = LoadTypeSpecs(getTypes());
                success = true;
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
            catch (InvalidOperationException ex)
            {
                //TODO Ignore For Now
            }
            return success;
        }

        public bool TryLoadTypeSpecs<TSpec>(Func<TypeReference[]> getTypes, out TSpec[] tSpecs)
        {
            bool success = TryLoadTypeSpecs(getTypes, out TypeSpec[] typeSpecs);
            tSpecs = success ? typeSpecs.Cast<TSpec>().ToArray() : Array.Empty<TSpec>();
            return success;
        }

        public TypeSpec[] LoadTypeSpecs(TypeReference[] types)
        {
            if (types.Any(t => t == null))
            {

            }
            return types.Select(t => LoadTypeSpec(t)).ToArray();
        }

        #endregion

        #region Method Specs

        public IReadOnlyDictionary<MethodDefinition, MethodSpec> Methods => _methodSpecs;

        ConcurrentDictionary<MethodDefinition, MethodSpec> _methodSpecs = new ConcurrentDictionary<MethodDefinition, MethodSpec>();
        
        public void ProcessLoadedMethods(bool includeSystem = true, bool parallelProcessing = true)
        {
            ProcessSpecs(Methods.Values.Where(t => includeSystem || !t.IsSystem), parallelProcessing);
        }

        public MethodSpec LoadMethodSpec(MethodReference method)
        {
            var methodDefinition = method as MethodDefinition;
            if (methodDefinition == null)
            {
                return new MissingMethodSpec(method, this);
                //try
                //{
                //    var module = LoadReferencedModuleByScopeName(method.Module, method.DeclaringType.Scope);
                //    var type = module.GetTypeSpec(method.DeclaringType);
                //    return type.MatchMethodReference(method);
                //}
                //catch
                //{
                //}                
            }
            return _methodSpecs.GetOrAdd(methodDefinition, (key) => CreateMethodSpec(methodDefinition));
        }

        public MethodSpec LoadMethodSpec(MethodDefinition method)
        {
            if (method == null)
            {
                return null;
            }
            return _methodSpecs.GetOrAdd(method, (key) => CreateMethodSpec(method));
        }

        private MethodSpec CreateMethodSpec(MethodDefinition method)
        {
            var spec = new MethodSpec(method, this);
            var synonyms = Methods.Where(m => m.Key.FullName == method.FullName && m.Key != method).ToArray();
            if (synonyms.Any())
            {
            }
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
            TryLoadTypeSpecs(() => parameterDefinition.CustomAttributes.Select(t => t.AttributeType).ToArray(), out TypeSpec[] typeSpecs);
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
                MethodReference methodReference => new MethodToMethodDependency(methodSpec, LoadMethodSpec(methodReference)),
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
