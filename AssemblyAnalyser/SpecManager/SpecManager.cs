using AssemblyAnalyser.Extensions;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
            _assemblySpecs.Clear();
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
            ProcessAllAssemblies(includeSystem, parallelProcessing);
            ProcessAllLoadedTypes(includeSystem, parallelProcessing);
            ProcessLoadedMethods(includeSystem, parallelProcessing);
            ProcessLoadedProperties(includeSystem);
            ProcessLoadedParameters(includeSystem);
            ProcessLoadedFields(includeSystem);
            ProcessLoadedEvents(includeSystem);
            ProcessLoadedAttributes(includeSystem);
        }

        #region Assemblies

        public IReadOnlyDictionary<string, AssemblySpec> Assemblies => _assemblySpecs;

        ConcurrentDictionary<string, AssemblySpec> _assemblySpecs = new ConcurrentDictionary<string, AssemblySpec>();

        public void ProcessAllAssemblies(bool includeSystem = true, bool parallelProcessing = true)
        {
            var list = new List<AssemblySpec>();
            
            var assemblySpecs = Assemblies.Values;

            var nonSystemAssemblies = assemblySpecs.Where(a => includeSystem || !a.IsSystemAssembly).ToArray();
            foreach (var assembly in nonSystemAssemblies)
            {
                RecursivelyLoadAssemblies(assembly, list);
            }
            list = list.Where(a => includeSystem || !a.IsSystemAssembly).ToList();
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
        
        private void RecursivelyLoadAssemblies(AssemblySpec assemblySpec, List<AssemblySpec> loaded)
        {
            var referencedAssemblies = assemblySpec.LoadReferencedAssemblies(false);
            if (!loaded.Contains(assemblySpec))
            {
                loaded.Add(assemblySpec);
            }
            if (referencedAssemblies.Any())
            {
                var newAssemblies = referencedAssemblies.Except(loaded);
                foreach (var newAssembly in newAssemblies)
                {
                    RecursivelyLoadAssemblies(newAssembly, loaded);
                }
            }
        }

        public AssemblySpec LoadAssemblySpec(Assembly assembly)
        {
            AssemblySpec assemblySpec;
            if (assembly == null)
            {
                return AssemblySpec.NullSpec;
            }
            assemblySpec = _assemblySpecs.GetOrAdd(assembly.FullName, (key) => CreateFullAssemblySpec(assembly));            
            return assemblySpec;
        }

        public AssemblySpec[] LoadReferencedAssemblies(string assemblyFullName, string assemblyFilePath, string targetFrameworkVersion = null, string imageRuntimeVersion = null)
        {
            var specs = new List<AssemblySpec>();
            var loader = AssemblyLoader.GetLoader(targetFrameworkVersion, imageRuntimeVersion);
            var assemblyNames = loader.PreLoadReferencedAssembliesByRootPath(assemblyFilePath);
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var assemblySpec = _assemblySpecs.GetOrAdd(assemblyName, (name) =>
                    {
                        var assembly = loader.LoadAssemblyByName(assemblyName);
                        return CreateFullAssemblySpec(assembly);
                    });
                    specs.Add(assemblySpec);
                }
                catch (FileNotFoundException ex)
                {
                    _exceptionManager.Handle(ex);
                    _logger.LogWarning($"Unable to load assembly {assemblyName}. Required by {assemblyFullName}");
                }
                catch
                {
                    _logger.LogWarning($"Unable to load assembly {assemblyName}. Required by {assemblyFullName}");
                }
            }
            return specs.OrderBy(s => s.FilePath).ToArray();
        }
        
        public AssemblySpec[] LoadAssemblySpecs(Assembly[] types)
        {
            return types.Select(t => LoadAssemblySpec(t)).ToArray();
        }

        private AssemblySpec CreateFullAssemblySpec(Assembly assembly)
        {
            assembly.TryGetTargetFrameworkVersion(out string frameworkVersion);
            var spec = new AssemblySpec(assembly.FullName, assembly.GetName().Name, assembly.Location, this, SpecRules)
            {
                ImageRuntimeVersion = assembly.ImageRuntimeVersion,
                TargetFrameworkVersion = frameworkVersion,
            };
            spec.Logger = _logger;
            return spec;
        }

        #endregion

        #region Modules

        public IReadOnlyDictionary<string, ModuleSpec> Modules => _moduleSpecs;

        ConcurrentDictionary<string, ModuleSpec> _moduleSpecs = new ConcurrentDictionary<string, ModuleSpec>();

        public void ProcessAllModules(bool includeSystem = true, bool parallelProcessing = true)
        {
            //var list = new List<AssemblySpec>();

            //var assemblySpecs = Assemblies.Values;

            //var nonSystemAssemblies = assemblySpecs.Where(a => includeSystem || !a.IsSystemAssembly).ToArray();
            //foreach (var assembly in nonSystemAssemblies)
            //{
            //    RecursivelyLoadAssemblies(assembly, list);
            //}
            //list = list.Where(a => includeSystem || !a.IsSystemAssembly).ToList();
            //if (parallelProcessing)
            //{
            //    Parallel.ForEach(list, l => l.Process());
            //}
            //else
            //{
            //    foreach (var item in list)
            //    {
            //        item.Process();
            //    }
            //}
        }

        public ModuleSpec LoadModuleSpec(ModuleDefinition module)
        {
            ModuleSpec assemblySpec;
            if (module == null)
            {
                throw new NotImplementedException();
                //return ModuleSpec.NullSpec;
            }
            assemblySpec = _moduleSpecs.GetOrAdd(module.Name, (key) => CreateFullModuleSpec(module));
            return assemblySpec;
        }

        public ModuleSpec[] LoadReferencedModules(ModuleDefinition module)
        {

            var specs = new List<ModuleSpec>();
            var locator = AssemblyLocator.GetLocator(module);
            foreach (var assemblyReference in module.AssemblyReferences)
            {
                try
                {
                    var assemblyLocation = locator.LocateAssemblyByName(assemblyReference.FullName);
                    if (string.IsNullOrEmpty(assemblyLocation))
                    {
                        _logger.LogWarning($"Asssembly not found {assemblyReference.FullName}");
                        continue;
                    }                    
                    var assemblySpec = _moduleSpecs.GetOrAdd(assemblyReference.FullName, (name) => CreateFullModuleSpec(assemblyLocation));
                    specs.Add(assemblySpec);
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
            }
            return specs.OrderBy(s => s.FilePath).ToArray();            
        }

        public ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] types)
        {
            return types.Select(t => LoadModuleSpec(t)).ToArray();
        }

        private ModuleSpec CreateFullModuleSpec(string filePath)
        {
            var module = ModuleDefinition.ReadModule(filePath);
            //module.TryGetTargetFrameworkVersion(out string frameworkVersion);
            return CreateFullModuleSpec(module);
        }

        private ModuleSpec CreateFullModuleSpec(ModuleDefinition module)
        {
            //module.TryGetTargetFrameworkVersion(out string frameworkVersion);
            var spec = new ModuleSpec(module, module.FileName, this, SpecRules)
            {

                //ImageRuntimeVersion = assembly.ImageRuntimeVersion,
                //TargetFrameworkVersion = frameworkVersion,
            };
            spec.Logger = _logger;
            return spec;
        }

        #endregion


        #region Types

        public IReadOnlyDictionary<string, TypeSpec> Types => _typeSpecs;

        ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public void ProcessAllLoadedTypes(bool includeSystem = true, bool parallelProcessing = true)
        {
            ProcessSpecs(Types.Values.Where(t => includeSystem || !t.Assembly.IsSystemAssembly).ToArray(), parallelProcessing);            
        }

        public void TryBuildTypeSpecForAssembly(string fullTypeName, string @namespace, string name, AssemblySpec assemblySpec, Action<TypeInfo> buildAction)
        {
            TypeInfo type = null;
            var loader = AssemblyLoader.GetLoader(assemblySpec.TargetFrameworkVersion, assemblySpec.ImageRuntimeVersion);
            var assembly = loader.LoadAssemblyByName(assemblySpec.AssemblyFullName);
            try
            {
                var definedTypes = assembly.DefinedTypes;
                type = definedTypes.SingleOrDefault(t => t.FullName == fullTypeName);
                if (type == null)
                {
                    var matches = definedTypes.Where(t => t.Name == name && t.Namespace == @namespace);
                    if (matches.Count() == 1)
                    {
                        type = matches.Single();
                    }
                    else
                    {

                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.SingleOrDefault(t => t.FullName == fullTypeName) as TypeInfo;
            }
            if (type == null)
            {
                _logger.LogError($"Could not find Type {fullTypeName}");
            }
            buildAction(type);
        }

        public TypeSpec[] TryLoadTypesForAssembly(AssemblySpec assemblySpec)
        {
            var specs = new List<TypeSpec>();
            var loader = AssemblyLoader.GetLoader(assemblySpec.TargetFrameworkVersion, assemblySpec.ImageRuntimeVersion);
            var assembly = loader.LoadAssemblyByName(assemblySpec.AssemblyFullName);
            TryLoadTypeSpecs(() => assembly.DefinedTypes.ToArray(), out TypeSpec[] typeSpecs, assemblySpec);
            return typeSpecs;
        }

        private TypeSpec LoadTypeSpec(Type type, AssemblySpec assemblySpec)
        {
            if (type == null)
            {
                return TypeSpec.NullSpec;
            }
            return LoadFullTypeSpec(type, assemblySpec);
        }

        private TypeSpec LoadFullTypeSpec(Type type, AssemblySpec assemblySpec = null)
        {
            return _typeSpecs.GetOrAdd(type.ToUniqueTypeName(), (key) => CreateFullTypeSpec(type, assemblySpec));
        }

        private TypeSpec CreateFullTypeSpec(Type type, AssemblySpec assemblySpec)
        {
            assemblySpec ??= LoadAssemblySpec(type.Assembly);
            var spec = new TypeSpec(type.FullName, type.ToUniqueTypeName(), type.IsInterface, assemblySpec, this, SpecRules)
            {
                Name = type.Name,
                Namespace = type.Namespace,
            };
            spec.Logger = _logger;            
            return spec;
        }

        //private TypeSpec LoadPartialTypeSpec(string typeName)
        //{
        //    return _typeSpecs.GetOrAdd(typeName, (key) => CreatePartialTypeSpec(typeName));            
        //}

        //private TypeSpec CreatePartialTypeSpec(string typeName)
        //{
        //    var spec = new TypeSpec(typeName, this, SpecRules);
        //    spec.Exclude("Type is only partial spec");
        //    spec.SkipProcessing("Type is only partial spec");
        //    spec.Logger = _logger;            
        //    return spec;
        //}

        public bool TryLoadTypeSpec(Func<Type> getType, out TypeSpec typeSpec, AssemblySpec assemblySpec = null)
        {
            bool success = false;
            try
            {
                typeSpec = LoadTypeSpec(getType(), assemblySpec);
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

        public bool TryLoadTypeSpecs(Func<Type[]> getTypes, out TypeSpec[] typeSpecs, AssemblySpec assemblySpec = null)
        {
            typeSpecs = Array.Empty<TypeSpec>();
            bool success = false;
            try
            { 
                typeSpecs = LoadTypeSpecs(getTypes(), assemblySpec);
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
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                if (ex.Types.Any())
                {
                    success = true;
                    typeSpecs = LoadTypeSpecs(ex.Types, assemblySpec);
                }
            }
            catch (InvalidOperationException ex)
            {
                //TODO Ignore For Now
            }
            return success;
        }

        public TypeSpec[] LoadTypeSpecs(Type[] types, AssemblySpec assemblySpec)
        {
            if (types.Any(t => t == null))
            {

            }
            return types.Select(t => LoadTypeSpec(t, assemblySpec)).ToArray();
        }

        #endregion

        #region Method Specs

        public IReadOnlyDictionary<MethodInfo, MethodSpec> Methods => _methodSpecs;

        ConcurrentDictionary<MethodInfo, MethodSpec> _methodSpecs = new ConcurrentDictionary<MethodInfo, MethodSpec>();
        
        public void ProcessLoadedMethods(bool includeSystem = true, bool parallelProcessing = true)
        {
            ProcessSpecs(Methods.Values.Where(t => includeSystem || !t.IsSystemMethod), parallelProcessing);
        }

        public MethodSpec LoadMethodSpec(MethodInfo method, TypeSpec declaringType)
        {
            if (method == null)
            {
                return null;
            }
            return _methodSpecs.GetOrAdd(method, (key) => CreateMethodSpec(method, declaringType));            
        }

        private MethodSpec CreateMethodSpec(MethodInfo method, TypeSpec declaringType)
        {
            declaringType ??= LoadFullTypeSpec(method.DeclaringType);
            var spec = new MethodSpec(method, declaringType, this, SpecRules)
            {
                Logger = _logger
            };
            return spec;
        }

        public MethodSpec[] LoadMethodSpecs(MethodInfo[] methodInfos, TypeSpec declaringType)
        {
            return methodInfos.Select(m => LoadMethodSpec(m, declaringType)).ToArray();
        }

        public MethodSpec[] TryLoadMethodSpecs(Func<MethodInfo[]> getMethods, TypeSpec declaringType)
        {
            MethodInfo[] methods = null;
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
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            finally
            {
                methods ??= Array.Empty<MethodInfo>();
            }
            return LoadMethodSpecs(methods, declaringType);
        }

        #endregion

        #region Property Specs
        
        public IReadOnlyDictionary<PropertyInfo, PropertySpec> Properties => _propertySpecs;

        ConcurrentDictionary<PropertyInfo, PropertySpec> _propertySpecs = new ConcurrentDictionary<PropertyInfo, PropertySpec>();

        public void ProcessLoadedProperties(bool includeSystem = true)
        {
            foreach (var (propertyName, prop) in Properties.Where(t => includeSystem || !t.Value.IsSystemProperty))
            {
                prop.Process();
            }
        }

        private PropertySpec LoadPropertySpec(PropertyInfo propertyInfo, TypeSpec declaringType)
        {
            PropertySpec propertySpec;
            if (!_propertySpecs.TryGetValue(propertyInfo, out propertySpec))
            {
                _propertySpecs[propertyInfo] = propertySpec = CreatePropertySpec(propertyInfo, declaringType);
            }
            return propertySpec;
        }

        private PropertySpec CreatePropertySpec(PropertyInfo propertyInfo, TypeSpec declaringType)
        {
            return new PropertySpec(propertyInfo, declaringType, this, SpecRules);
        }

        public PropertySpec[] LoadPropertySpecs(PropertyInfo[] propertyInfos, TypeSpec declaringType)
        {
            return propertyInfos.Select(p => LoadPropertySpec(p, declaringType)).ToArray();
        }

        public PropertySpec[] TryLoadPropertySpecs(Func<PropertyInfo[]> getProperties, TypeSpec declaringType)
        {
            PropertyInfo[] properties = null;
            try
            {
                properties = getProperties();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                properties ??= Array.Empty<PropertyInfo>();
            }
            return LoadPropertySpecs(properties, declaringType);
        }

        #endregion

        #region Parameter Specs

        public IReadOnlyDictionary<ParameterInfo, ParameterSpec> Parameters => _parameterSpecs;

        ConcurrentDictionary<ParameterInfo, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<ParameterInfo, ParameterSpec>();

        public void ProcessLoadedParameters(bool includeSystem = true)
        {
            foreach (var (parameterName, param) in Parameters.Where(t => includeSystem || !t.Value.IsSystemParameter))
            {
                param.Process();
            }
        }

        private ParameterSpec LoadParameterSpec(ParameterInfo parameterInfo, MethodSpec method)
        {
            return _parameterSpecs.GetOrAdd(parameterInfo, CreateParameterSpec(parameterInfo, method));
        }

        private ParameterSpec CreateParameterSpec(ParameterInfo parameterInfo, MethodSpec method)
        {
            TryLoadTypeSpecs(() => parameterInfo.GetCustomAttributesData().Select(t => t.AttributeType).ToArray(), out TypeSpec[] typeSpecs);
            return new ParameterSpec(parameterInfo, method, this, SpecRules);
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterInfo[] parameterInfos, MethodSpec method)
        {
            return parameterInfos?.Select(p => LoadParameterSpec(p, method)).ToArray();
        }

        public ParameterSpec[] TryLoadParameterSpecs(Func<ParameterInfo[]> parameterInfosFunc, MethodSpec method)
        {
            ParameterInfo[] parameterInfos = null;
            try
            {
                parameterInfos = parameterInfosFunc();
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

        public IReadOnlyDictionary<FieldInfo, FieldSpec> Fields => _fieldSpecs;

        ConcurrentDictionary<FieldInfo, FieldSpec> _fieldSpecs = new ConcurrentDictionary<FieldInfo, FieldSpec>();

        public void ProcessLoadedFields(bool includeSystem = true)
        {
            foreach (var (fieldName, field) in Fields.Where(t => includeSystem || !t.Value.IsSystemField))
            {
                field.Process();
            }
        }

        private FieldSpec LoadFieldSpec(FieldInfo fieldInfo, TypeSpec declaringType)
        {
            FieldSpec fieldSpec = _fieldSpecs.GetOrAdd(fieldInfo, (spec) => CreateFieldSpec(fieldInfo, declaringType));
            //if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
            //{
            //    //Console.WriteLine($"Locking for {fieldInfo.Name}");
            //    lock (_lock)
            //    {
            //        if (!_fieldSpecs.TryGetValue(fieldInfo, out fieldSpec))
            //        {
            //            _fieldSpecs[fieldInfo] = fieldSpec = CreateFieldSpec(fieldInfo, declaringType);
            //        }
            //    }
            //    //Console.WriteLine($"Unlocking for {fieldInfo.Name}");
            //}
            return fieldSpec;
        }

        private FieldSpec CreateFieldSpec(FieldInfo fieldInfo, TypeSpec declaringType)
        {
            return new FieldSpec(fieldInfo, declaringType, this, SpecRules);
        }

        public FieldSpec[] LoadFieldSpecs(FieldInfo[] fieldInfos, TypeSpec declaringType)
        {
            return fieldInfos.Select(f => LoadFieldSpec(f, declaringType)).ToArray();
        }

        public FieldSpec[] TryLoadFieldSpecs(Func<FieldInfo[]> getFields, TypeSpec declaringType)
        {
            FieldInfo[] fields = null;
            try
            {
                fields = getFields();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                fields ??= Array.Empty<FieldInfo>();
            }
            return LoadFieldSpecs(fields, declaringType);
        }

        #endregion

        #region Attribute Specs

        public IReadOnlyDictionary<string, TypeSpec> Attributes => _attributeSpecs;

        ConcurrentDictionary<string, TypeSpec> _attributeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttributeData[]> getAttributes, AbstractSpec decoratedSpec)
        {
            CustomAttributeData[] attributes = null;
            try
            {
                attributes = getAttributes();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                attributes ??= Array.Empty<CustomAttributeData>();
            }
            return LoadAttributeSpecs(attributes, decoratedSpec);
        }

        public TypeSpec[] LoadAttributeSpecs(CustomAttributeData[] attibutes, AbstractSpec decoratedSpec)
        {
            return attibutes.Select(f => LoadAttributeSpec(f, decoratedSpec)).ToArray();
        }

        private TypeSpec LoadAttributeSpec(CustomAttributeData attribute, AbstractSpec decoratedSpec)
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

        public IReadOnlyDictionary<EventInfo, EventSpec> Events => _eventSpecs;

        ConcurrentDictionary<EventInfo, EventSpec> _eventSpecs = new ConcurrentDictionary<EventInfo, EventSpec>();

        public void ProcessLoadedEvents(bool includeSystem = true)
        {
            foreach (var (fieldName, field) in Events.Where(t => includeSystem || !t.Value.IsSystemEvent))
            {
                field.Process();
            }
        }

        public EventSpec[] TryLoadEventSpecs(Func<EventInfo[]> getEvents, TypeSpec declaringType)
        {
            EventInfo[] events = null;
            try
            {
                events = getEvents();
            }
            catch (TypeLoadException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                _logger.LogError(ex, "File Not Found");
            }
            finally
            {
                events ??= Array.Empty<EventInfo>();
            }
            return LoadEventSpecs(events, declaringType);
        }

        private EventSpec LoadEventSpec(EventInfo eventInfo, TypeSpec declaringType)
        {
            EventSpec fieldSpec = _eventSpecs.GetOrAdd(eventInfo, (e) => CreateEventSpec(eventInfo, declaringType));
            return fieldSpec;
        }

        private EventSpec CreateEventSpec(EventInfo eventInfo, TypeSpec declaringType)
        {
            return new EventSpec(eventInfo, declaringType, this, SpecRules);
        }

        public EventSpec[] LoadEventSpecs(EventInfo[] eventInfos, TypeSpec declaringType)
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
