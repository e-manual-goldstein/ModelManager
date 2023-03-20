using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class SpecManager : ISpecManager
    {
        Dictionary<string, string> _workingFiles;
        readonly ILogger _logger;
        readonly IExceptionManager _exceptionManager;
        object _lock = new object();
        private bool _disposed;

        public SpecManager(ILoggerProvider loggerProvider, IExceptionManager exceptionManager)
        {            
            _logger = loggerProvider.CreateLogger("Spec Manager");
            _exceptionManager = exceptionManager;
        }
        
        public List<IRule> SpecRules { get; set; } = new List<IRule>();

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingFiles = Directory.EnumerateFiles(workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
        }

        public void Reset()
        {
            _moduleSpecs.Clear();
            _typeSpecs.Clear();
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
            ProcessAllLoadedTypes(includeSystem, parallelProcessing);
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

        public ModuleSpec[] LoadReferencedModules(ModuleDefinition baseModule)
        {
            var specs = new List<ModuleSpec>();
            var locator = AssemblyLocator.GetLocator(baseModule);
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {
                var moduleSpec = LoadReferencedModule(baseModule, assemblyReference.FullName);
                if (moduleSpec != null)
                {
                    specs.Add(moduleSpec);
                }
            }
            return specs.OrderBy(s => s.FilePath).ToArray();            
        }

        public ModuleSpec LoadReferencedModule(ModuleDefinition module, string referencedModuleName)
        {
            var locator = AssemblyLocator.GetLocator(module);
            var assemblyReference = module.AssemblyReferences.Single(a => a.FullName.Contains(referencedModuleName));
            try
            {
                var assemblyLocation = locator.LocateAssemblyByName(assemblyReference.FullName);
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    _logger.LogWarning($"Asssembly not found {assemblyReference.FullName}");      
                    return null;
                }
                var referencedModule = ModuleDefinition.ReadModule(assemblyLocation);
                var moduleSpec = _moduleSpecs.GetOrAdd(assemblyReference.Name, (name) => CreateFullModuleSpec(referencedModule));
                moduleSpec.AddModuleVersion(assemblyReference);
                return moduleSpec;
                
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogWarning($"Unable to load assembly {assemblyReference.FullName}. Required by {module}");
            }
            catch
            {
                _logger.LogWarning($"Unable to load assembly {assemblyReference.FullName}. Required by {module}");
            }

            return null;
        }

        public ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] types)
        {
            return types.Select(t => LoadModuleSpec(t)).ToArray();
        }

        private ModuleSpec CreateFullModuleSpec(ModuleDefinition module)
        {
            var spec = new ModuleSpec(module, module.FileName, this, SpecRules)
            {

            };
            spec.Logger = _logger;
            return spec;
        }

        #endregion

        #region Types

        public TypeSpec[] ProcessedTypes => Types.Values.Where(t => t.IsProcessed).OrderBy(s => s.FullTypeName).ToArray();

        public IReadOnlyDictionary<string, TypeSpec> Types => _typeSpecs;

        ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public void ProcessAllLoadedTypes(bool includeSystem = true, bool parallelProcessing = true)
        {
            ProcessSpecs(Types.Values.Where(t => includeSystem || !t.Module.IsSystem).ToArray(), parallelProcessing);            
        }

        public TypeSpec[] TryLoadTypesForModule(ModuleDefinition module)
        {
            var specs = new List<TypeSpec>();
            var types = module.GetTypes();
            TryLoadTypeSpecs(() => types.ToArray(), out TypeSpec[] typeSpecs);
            
            return typeSpecs;
        }

        private TypeSpec LoadTypeSpec(TypeReference type)
        {
            if (type == null)
            {
                return TypeSpec.NullSpec;
            }
            return LoadFullTypeSpec(type);
        }

        private TypeSpec LoadFullTypeSpec(TypeDefinition type)
        {
            var typeSpec = _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type), (key) => CreateFullTypeSpec(type));
            if (!typeSpec.HasDefinition)
            {
                typeSpec.AddDefinition(type);
            }
            return typeSpec;
        }

        private string CreateUniqueTypeSpecName(TypeReference type)
        {
            return $"{type.Module.Name}_{type.FullName}";
        }

        private TypeSpec LoadFullTypeSpec(TypeReference type)
        {
            return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type), (key) => CreateFullTypeSpec(type));
        }

        private TypeSpec CreateFullTypeSpec(TypeReference type, ModuleSpec moduleSpec = null)
        {
            moduleSpec ??= LoadModuleSpec(type.Module);
            var spec = new TypeSpec(type, moduleSpec, this, SpecRules)
            {
                Name = type.Name,
                Namespace = type.Namespace,
            };
            spec.Logger = _logger;
            return spec;
        }

        public bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec)
        {
            bool success = false;
            try
            {
                typeSpec = LoadTypeSpec(getType());
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
                _logger.LogError(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                //TODO Ignore For Now
            }
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
            ProcessSpecs(Methods.Values.Where(t => includeSystem || !t.IsSystemMethod), parallelProcessing);
        }

        public MethodSpec LoadMethodSpec(MethodDefinition method, TypeSpec declaringType)
        {
            if (method == null)
            {
                return null;
            }
            return _methodSpecs.GetOrAdd(method, (key) => CreateMethodSpec(method, declaringType));
        }

        private MethodSpec CreateMethodSpec(MethodDefinition method, TypeSpec declaringType)
        {
            declaringType ??= LoadFullTypeSpec(method.DeclaringType);
            var spec = new MethodSpec(method, declaringType, this, SpecRules)
            {
                Logger = _logger
            };
            return spec;
        }

        public MethodSpec[] LoadMethodSpecs(MethodDefinition[] methodDefinitions, TypeSpec declaringType)
        {
            return methodDefinitions.Select(m => LoadMethodSpec(m, declaringType)).ToArray();
        }

        public MethodSpec[] TryLoadMethodSpecs(Func<MethodDefinition[]> getMethods, TypeSpec declaringType)
        {
            MethodDefinition[] methods = null;
            try
            {
                methods = getMethods();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex.Message);
            }
            finally
            {
                methods ??= Array.Empty<MethodDefinition>();
            }
            return LoadMethodSpecs(methods, declaringType);
        }


        #endregion

        #region Property Specs

        public IReadOnlyDictionary<PropertyDefinition, PropertySpec> Properties => _propertySpecs;

        ConcurrentDictionary<PropertyDefinition, PropertySpec> _propertySpecs = new ConcurrentDictionary<PropertyDefinition, PropertySpec>();

        public void ProcessLoadedProperties(bool includeSystem = true)
        {
            foreach (var (propertyName, prop) in Properties.Where(t => includeSystem || !t.Value.IsSystemProperty))
            {
                prop.Process();
            }
        }

        private PropertySpec LoadPropertySpec(PropertyDefinition propertyInfo, TypeSpec declaringType)
        {
            PropertySpec propertySpec;
            if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
            {
                _propertySpecs[propertyInfo] = propertySpec = CreatePropertySpec(propertyInfo, declaringType);
            }
            return propertySpec;
        }

        private PropertySpec CreatePropertySpec(PropertyDefinition propertyInfo, TypeSpec declaringType)
        {
            return new PropertySpec(propertyInfo, declaringType, this, SpecRules);
        }

        public PropertySpec[] LoadPropertySpecs(PropertyDefinition[] propertyInfos, TypeSpec declaringType)
        {
            return propertyInfos.Select(p => LoadPropertySpec(p, declaringType)).ToArray();
        }

        public PropertySpec[] TryLoadPropertySpecs(Func<PropertyDefinition[]> getProperties, TypeSpec declaringType)
        {
            PropertyDefinition[] properties = null;
            try
            {
                properties = getProperties();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
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
            return LoadPropertySpecs(properties, declaringType);
        }

        #endregion

        #region Parameter Specs

        public IReadOnlyDictionary<ParameterDefinition, ParameterSpec> Parameters => _parameterSpecs;

        ConcurrentDictionary<ParameterDefinition, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<ParameterDefinition, ParameterSpec>();

        public void ProcessLoadedParameters(bool includeSystem = true)
        {
            foreach (var (parameterName, param) in Parameters.Where(t => includeSystem || !t.Value.IsSystemParameter))
            {
                param.Process();
            }
        }

        private ParameterSpec LoadParameterSpec(ParameterDefinition parameterDefinition, MethodSpec method)
        {
            return _parameterSpecs.GetOrAdd(parameterDefinition, CreateParameterSpec(parameterDefinition, method));
        }

        private ParameterSpec CreateParameterSpec(ParameterDefinition parameterDefinition, MethodSpec method)
        {
            TryLoadTypeSpecs(() => parameterDefinition.CustomAttributes.Select(t => t.AttributeType).ToArray(), out TypeSpec[] typeSpecs);
            return new ParameterSpec(parameterDefinition, method, this, SpecRules);
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterDefinition[] parameterDefinitions, MethodSpec method)
        {
            return parameterDefinitions?.Select(p => LoadParameterSpec(p, method)).ToArray();
        }

        public ParameterSpec[] TryLoadParameterSpecs(Func<ParameterDefinition[]> parameterDefinitions, MethodSpec method)
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
            return LoadParameterSpecs(parameterInfos, method);
        }

        #endregion

        #region Field Specs

        public IReadOnlyDictionary<FieldDefinition, FieldSpec> Fields => _fieldSpecs;

        ConcurrentDictionary<FieldDefinition, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldDefinition, FieldSpec>();

        public void ProcessLoadedFields(bool includeSystem = true)
        {
            foreach (var (fieldName, field) in Fields.Where(t => includeSystem || !t.Value.IsSystemField))
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
            return new FieldSpec(fieldInfo, declaringType, this, SpecRules);
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
                _logger.LogError(ex.Message);
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
                _logger.LogError(ex.Message);
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
            var attributeSpec = LoadFullTypeSpec(attribute.AttributeType);
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
            foreach (var (fieldName, field) in Events.Where(t => includeSystem || !t.Value.IsSystemEvent))
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
                _logger.LogError(ex.Message);
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
            return new EventSpec(eventInfo, declaringType, this, SpecRules);
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

        #endregion

    }
}
