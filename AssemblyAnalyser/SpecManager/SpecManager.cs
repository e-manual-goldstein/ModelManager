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
using AssemblyAnalyser.Faults;

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
            ScopeExtended += (sender, args) =>
            {
                if (!_assemblyResolver.GetSearchDirectories().Contains(args.ScopeDirectory))
                {
                    _logger.LogInformation("Extending Scope");
                    _assemblyResolver.AddSearchDirectory(args.ScopeDirectory);
                }
            };
            return resolver;
        }

        public IAssemblyResolver AssemblyResolver => _assemblyResolver;

        public event ScopeExtendedEventHandler ScopeExtended;

        #region Faults

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

        public void ClearFaults()
        {
            _faults.Clear();
        }

        private void LogFault(FaultSeverity? faultSeverity, string faultMessage)
        {
            switch (faultSeverity)
            {
                case FaultSeverity.Critical:
                    _logger.Log(LogLevel.Critical, faultMessage);
                    break;
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
        #endregion

        #region Rules
        
        List<IRule> _specRules = new List<IRule>();
        public IRule[] SpecRules => _specRules.ToArray();

        public void AddRule(IRule specRule)
        {
            _specRules.Add(specRule);
        }

        #endregion

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingFiles = Directory.EnumerateFiles(workingDirectory, "*.dll").ToDictionary(d => Path.GetFileNameWithoutExtension(d), e => e);
        }

        public void Reset()
        {
            //_moduleSpecs.Clear();
            //_methodSpecs.Clear();
            //_parameterSpecs.Clear();
            //_propertySpecs.Clear();
            //_fieldSpecs.Clear();
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
            //ProcessLoadedProperties(includeSystem);
            //ProcessLoadedParameters(includeSystem);
            //ProcessLoadedFields(includeSystem);
            //ProcessLoadedEvents(includeSystem);
            ProcessLoadedAttributes(includeSystem);
        }

        #region Assembly Specs

        public IReadOnlyDictionary<string, AssemblySpec> AssemblySpecs => _assemblies;

        ConcurrentDictionary<string, AssemblySpec> _assemblies = new ConcurrentDictionary<string, AssemblySpec>();

        public AssemblySpec LoadAssemblySpecFromPath(string assemblySpecPath)
        {
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = _assemblyResolver
            };
            var moduleDefinition = AssemblyDefinition.ReadAssembly(assemblySpecPath, readerParameters);
            IAssemblyLocator assemblyLocator = AssemblyLocator.GetLocator(moduleDefinition.MainModule);
            return LoadAssemblySpec(moduleDefinition.Name, assemblySpecPath, assemblyLocator);
        }

        public IEnumerable<AssemblySpec> TryLoadReferencedAssemblies(ModuleDefinition moduleDefinition, IAssemblyLocator assemblyLocator)
        {
            foreach (var assemblyNameReference in moduleDefinition.AssemblyReferences)
            {
                yield return LoadReferencedAssemblyByFullName(moduleDefinition, assemblyNameReference, assemblyLocator);
            }
        }

        public AssemblySpec LoadReferencedAssemblyByFullName(ModuleDefinition module, AssemblyNameReference assemblyNameReference, IAssemblyLocator assemblyLocator)
        {
            if (module.GetScopeNameWithoutExtension() == assemblyNameReference.GetUniqueNameFromScope())
            {
                return LoadAssemblySpec(module.Assembly.Name, module.FileName, assemblyLocator);
            }
            return LoadReferencedAssembly(assemblyLocator, assemblyNameReference);
        }

        private AssemblySpec LoadReferencedAssembly(IAssemblyLocator locator, AssemblyNameReference assemblyReference)
        {
            try
            {
                var assemblyLocation = locator.LocateAssemblyByName(assemblyReference.FullName);
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    if (SystemModuleSpec.IsSystemModule(assemblyReference))
                    {
                        return _assemblies[SystemAssemblySpec.SYSTEM_ASSEMBLY_NAME];
                    }
                    return _assemblies
                        .GetOrAdd(assemblyReference.Name, (key) => new MissingAssemblySpec(assemblyReference, this));                    
                }
                var assemblySpec = LoadAssemblySpec(assemblyReference, assemblyLocation, locator);
                return assemblySpec;
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                AddFault(FaultSeverity.Error, $"Unable to load assembly {assemblyReference.FullName}. Required by {assemblyReference}");
            }
            return null;
        }

        public AssemblySpec LoadAssemblySpec(IMetadataScope assemblyNameReference, string filePath, IAssemblyLocator assemblyLocator)
        {
            var assemblyKey = SystemModuleSpec.IsSystemModule(assemblyNameReference)
                ? SystemAssemblySpec.SYSTEM_ASSEMBLY_NAME
                : assemblyNameReference.GetScopeNameWithoutExtension();
            return _assemblies.GetOrAdd(assemblyKey, (key) => CreateFullAssemblySpec(filePath, assemblyLocator))
                .RegisterMetaDataScope(assemblyNameReference);
        }

        private AssemblySpec CreateFullAssemblySpec(string filePath, IAssemblyLocator assemblyLocator)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(filePath);
            ScopeExtended(this, new ScopeExtendedEventArgs(filePath));
            if (SystemModuleSpec.IsSystemModule(assemblyDefinition.Name))
            {
                return new SystemAssemblySpec(assemblyDefinition, filePath, assemblyLocator, this);
            }
            return new AssemblySpec(assemblyDefinition, filePath, assemblyLocator, this);
        }

        #endregion

        #region Module Specs

        public ModuleSpec[] ModuleSpecs => _assemblies.Values.SelectMany(a => a.Modules).ToArray();

        public ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference, IAssemblyLocator assemblyLocator)
        {
            if (typeReference == null)
            {
                throw new NotImplementedException();
            }
            var assemblyNameReference = typeReference.Scope.GetAssemblyNameReferenceForScope();
            if (assemblyNameReference == null)
            {

            }
            var assemblySpec = LoadReferencedAssemblyByFullName(typeReference.Module, assemblyNameReference, assemblyLocator);
            return assemblySpec.LoadModuleSpecForTypeReference(typeReference);
        }

        //public ModuleSpec LoadModuleSpecFromPath(string moduleFilePath)
        //{
        //    var readerParameters = new ReaderParameters()
        //    {
        //        AssemblyResolver = _assemblyResolver
        //    };
        //    var moduleDefinition = ModuleDefinition.ReadModule(moduleFilePath, readerParameters);
        //    return LoadModuleSpec(moduleDefinition);
        //}

        public IEnumerable<ModuleSpec> LoadReferencedModules(ModuleDefinition baseModule, IAssemblyLocator assemblyLocator)
        {
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {
                yield return LoadReferencedModuleByFullName(baseModule, assemblyReference, assemblyLocator);
            }            
        }

        public ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, AssemblyNameReference assemblyNameReference, 
            IAssemblyLocator assemblyLocator)
        {
            if (module.GetScopeNameWithoutExtension() == assemblyNameReference.FullName)
            {
                return LoadReferencedAssemblyByFullName(module, assemblyNameReference, assemblyLocator).LoadModuleSpec(module);
            }
            if (assemblyLocator == null)
            {
                assemblyLocator ??= AssemblyLocator.GetLocator(module);
            }
            return LoadReferencedModule(assemblyLocator, assemblyNameReference);
        }

        private ModuleSpec LoadReferencedModule(IAssemblyLocator locator, AssemblyNameReference assemblyReference)
        {
            return LoadReferencedAssembly(locator, assemblyReference).LoadModuleSpec(assemblyReference);
        }

        //public ModuleSpec[] LoadModuleSpecs(ModuleDefinition[] modules)
        //{
        //    return modules.Select(t => LoadModuleSpec(t)).ToArray();
        //}

        //private ModuleSpec CreateFullModuleSpec(IMetadataScope scope)
        //{
        //    try
        //    {
        //        return scope switch
        //        {
        //            AssemblyNameDefinition assemblyNameDefinition => new MissingModuleSpec(assemblyNameDefinition, this),
        //            ModuleDefinition moduleDefinition => SystemModuleSpec.IsSystemModule(scope) 
        //                ? new SystemModuleSpec(moduleDefinition, moduleDefinition.FileName, this)
        //                : new ModuleSpec(moduleDefinition, moduleDefinition.FileName, this),
        //            AssemblyNameReference assemblyNameReference => LoadModuleByAssemblyNameReference(assemblyNameReference),
        //            _ => throw new NotImplementedException()
        //        };                
        //    }
        //    catch
        //    {

        //    }
        //    return new MissingModuleSpec(scope as AssemblyNameReference, this);
        //}

        //private ModuleSpec LoadModuleByAssemblyNameReference(AssemblyNameReference assemblyNameReference)
        //{
        //    return new MissingModuleSpec(assemblyNameReference, this);
        //}

        //private ModuleSpec CreateMissingModuleSpec(AssemblyNameReference assemblyNameReference)
        //{
        //    var spec = new MissingModuleSpec(assemblyNameReference, this);
        //    return spec;
        //}

        #endregion

        #region Type Specs

        static NullTypeSpec _nullTypeSpec;

        public TypeSpec GetNullTypeSpec()
        {
            return _nullTypeSpec ??= new NullTypeSpec(this);
        }

        public TypeSpec[] TypeSpecs => ModuleSpecs.SelectMany(m => m.TypeSpecs).ToArray();

        public TypeSpec LoadTypeSpec(TypeReference typeReference, IAssemblyLocator assemblyLocator)
        {
            if (typeReference == null)
            {
                return GetNullTypeSpec();
            }
            ModuleSpec module = LoadModuleSpecForTypeReference(typeReference, assemblyLocator);
            return module.LoadTypeSpec(typeReference);
        }

        public IEnumerable<TypeSpec> LoadTypeSpecs(IEnumerable<TypeReference> types, IAssemblyLocator assemblyLocator)
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference, assemblyLocator);
            }
        }

        public IEnumerable<TSpec> LoadTypeSpecs<TSpec>(IEnumerable<TypeReference> types, IAssemblyLocator assemblyLocator) 
            where TSpec : TypeSpec
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference, assemblyLocator) as TSpec;
            }
        }

        #endregion

        #region Method Specs

        public MethodSpec[] MethodSpecs => TypeSpecs.SelectMany(t => t.Methods).ToArray();

        public MethodSpec LoadMethodSpec(MethodDefinition method, bool allowNull, IAssemblyLocator assemblyLocator)
        {
            if (method == null)
            {
                AddFault(allowNull ? FaultSeverity.Debug : FaultSeverity.Warning, "No MethodSpec for null MethodDefintion");
                return null;
            }
            return LoadTypeSpec(method.DeclaringType, assemblyLocator).LoadMethodSpec(method);
        }

        public MethodSpec LoadMethodSpec(MethodReference method, bool allowNull, IAssemblyLocator assemblyLocator)
        {
            if (method == null)
            {
                AddFault(allowNull ? FaultSeverity.Debug : FaultSeverity.Warning, "No MethodSpec for null MethodDefintion");
                return null;
            }
            return LoadTypeSpec(method.DeclaringType, assemblyLocator).LoadMethodSpec(method);
        }

        public IEnumerable<MethodSpec> LoadSpecsForMethodReferences(IEnumerable<MethodReference> methodReferences, IAssemblyLocator assemblyLocator)
        {
            foreach (var methodReference in methodReferences)
            {
                if (methodReference is MethodDefinition methodDefinition)
                {
                    yield return LoadMethodSpec(methodDefinition, true, assemblyLocator);
                }
                else if (methodReference.DeclaringType?.Scope != null)
                {
                    yield return LoadMethodSpec(methodReference, true, assemblyLocator);
                }
                else
                {
                    try
                    {
                        methodDefinition = methodReference.Resolve();
                    }
                    catch
                    {
                        continue;
                    }
                    yield return LoadMethodSpec(methodDefinition, true, assemblyLocator);
                }
            }            
        }

        #endregion

        #region Property Specs

        public PropertySpec[] PropertySpecs => TypeSpecs.SelectMany(t => t.Properties).ToArray();

        public PropertySpec LoadPropertySpec(PropertyReference propertyReference, bool allowNull, IAssemblyLocator assemblyLocator)
        {
            if (propertyReference == null)
            {
                AddFault(allowNull ? FaultSeverity.Debug : FaultSeverity.Warning, "No PropertySpec for null PropertyDefinition");
                return null;
            }
            return LoadTypeSpec(propertyReference.DeclaringType, assemblyLocator).LoadPropertySpec(propertyReference.Resolve());
        }

        public IEnumerable<PropertySpec> LoadPropertySpecs(IEnumerable<PropertyReference> propertyReferences, IAssemblyLocator assemblyLocator)
        {
            foreach (var propertyReference in propertyReferences)
            {
                yield return LoadPropertySpec(propertyReference, true, assemblyLocator);
            }
        }

        #endregion

        #region Field Specs

        public FieldSpec[] FieldSpecs => TypeSpecs.SelectMany(t => t.Fields).ToArray();

        #endregion

        #region Event Specs

        public EventSpec[] EventSpecs => TypeSpecs.SelectMany(t => t.Events).ToArray();

        #endregion

        #region Attribute Specs

        public IReadOnlyDictionary<string, TypeSpec> Attributes => _attributeSpecs;

        ConcurrentDictionary<string, TypeSpec> _attributeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttribute[]> getAttributes, AbstractSpec decoratedSpec, IAssemblyLocator assemblyLocator)
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
            return LoadAttributeSpecs(attributes, decoratedSpec, assemblyLocator);
        }

        public TypeSpec[] LoadAttributeSpecs(CustomAttribute[] attibutes, AbstractSpec decoratedSpec, IAssemblyLocator assemblyLocator)
        {
            return attibutes.Select(f => LoadAttributeSpec(f, decoratedSpec, assemblyLocator)).ToArray();
        }

        private TypeSpec LoadAttributeSpec(CustomAttribute attribute, AbstractSpec decoratedSpec, IAssemblyLocator assemblyLocator)
        {
            var attributeSpec = LoadTypeSpec(attribute.AttributeType, assemblyLocator);
            _attributeSpecs.GetOrAdd(attributeSpec.FullTypeName, attributeSpec);
            attributeSpec.RegisterAsDecorator(decoratedSpec);
            return attributeSpec;
        }

        public void ProcessLoadedAttributes(bool includeSystem = true)
        {
            throw new NotImplementedException();
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
                //TypeReference typeReference => new MethodToTypeDependency(methodSpec, LoadTypeSpec(typeReference)),
                //MethodReference methodReference => new MethodToMethodDependency(methodSpec, LoadMethodSpec(methodReference)),
                //FieldReference fieldReference => fieldReference.Module,
                //ParameterReference parameterReference => parameterReference.ParameterType.Module,
                //VariableReference variableReference => variableReference.VariableType.Module,
                //Instruction operandInstruction => operandInstruction.Operand,
                //Instruction[] operandInstructions => operandInstructions.Select(t => t.Operand),
                _ => null
            };
            
        }
    }
}
