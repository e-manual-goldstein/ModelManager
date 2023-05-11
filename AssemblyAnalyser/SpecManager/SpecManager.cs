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
using Mono.Cecil.Cil;

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

        public void ProcessSpecs<TSpec>(IEnumerable<TSpec> specs, bool parallelProcessing = false) where TSpec : AbstractSpec
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
            return LoadAssemblySpec(moduleDefinition.Name, assemblySpecPath, new SpecContext(assemblyLocator));
        }

        public IEnumerable<AssemblySpec> TryLoadReferencedAssemblies(ModuleDefinition moduleDefinition, ISpecContext specContext)
        {
            foreach (var assemblyNameReference in moduleDefinition.AssemblyReferences)
            {
                yield return LoadReferencedAssemblyByFullName(moduleDefinition, assemblyNameReference, specContext);
            }
        }

        public AssemblySpec LoadReferencedAssemblyByFullName(ModuleDefinition module, AssemblyNameReference assemblyNameReference, ISpecContext specContext)
        {
            if (module.GetScopeNameWithoutExtension() == assemblyNameReference.GetUniqueNameFromScope())
            {
                return LoadAssemblySpec(module.Assembly.Name, module.FileName, specContext);
            }
            return LoadReferencedAssembly(specContext, assemblyNameReference);
        }

        private AssemblySpec LoadReferencedAssembly(ISpecContext specContext, AssemblyNameReference assemblyReference)
        {
            try
            {
                var assemblyLocation = specContext.AssemblyLocator.LocateAssemblyByName(assemblyReference.FullName);
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    if (SystemModuleSpec.IsSystemModule(assemblyReference))
                    {
                        return (_assemblies[SystemAssemblySpec.SYSTEM_ASSEMBLY_NAME] as SystemAssemblySpec).WithReferencedAssembly(assemblyReference);
                    }
                    return _assemblies
                        .GetOrAdd(assemblyReference.Name, (key) => new MissingAssemblySpec(assemblyReference, this, specContext));                    
                }
                var assemblySpec = LoadAssemblySpec(assemblyReference, assemblyLocation, specContext);
                return assemblySpec;
            }
            catch (FileNotFoundException ex)
            {
                _exceptionManager.Handle(ex);
                AddFault(FaultSeverity.Error, $"Unable to load assembly {assemblyReference.FullName}. Required by {assemblyReference}");
            }
            return null;
        }

        public AssemblySpec LoadAssemblySpec(IMetadataScope assemblyNameReference, string filePath, ISpecContext specContext)
        {
            var assemblyKey = SystemModuleSpec.IsSystemModule(assemblyNameReference)
                ? SystemAssemblySpec.SYSTEM_ASSEMBLY_NAME
                : assemblyNameReference.GetScopeNameWithoutExtension();
            return _assemblies.GetOrAdd(assemblyKey, (key) => CreateFullAssemblySpec(filePath, specContext))
                .RegisterMetaDataScope(assemblyNameReference);
        }

        private AssemblySpec CreateFullAssemblySpec(string filePath, ISpecContext specContext)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(filePath);
            ScopeExtended(this, new ScopeExtendedEventArgs(filePath));
            if (SystemModuleSpec.IsSystemModule(assemblyDefinition.Name))
            {
                return new SystemAssemblySpec(assemblyDefinition, filePath, this, specContext);
            }
            return new AssemblySpec(assemblyDefinition, filePath, this, specContext);
        }

        #endregion

        #region Module Specs

        public ModuleSpec[] ModuleSpecs => _assemblies.Values.SelectMany(a => a.Modules).ToArray();

        public ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference, ISpecContext specContext)
        {
            if (typeReference == null)
            {
                throw new NotImplementedException();
            }
            var assemblyNameReference = typeReference.Scope.GetAssemblyNameReferenceForScope();
            if (assemblyNameReference == null)
            {

            }
            var assemblySpec = LoadReferencedAssemblyByFullName(typeReference.Module, assemblyNameReference, specContext);
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

        public IEnumerable<ModuleSpec> LoadReferencedModules(ModuleDefinition baseModule, ISpecContext specContext)
        {
            foreach (var assemblyReference in baseModule.AssemblyReferences)
            {
                yield return LoadReferencedModuleByFullName(baseModule, assemblyReference, specContext);
            }            
        }

        public ModuleSpec LoadReferencedModuleByFullName(ModuleDefinition module, AssemblyNameReference assemblyNameReference,
            ISpecContext specContext)
        {
            if (module.GetScopeNameWithoutExtension() == assemblyNameReference.FullName)
            {
                return LoadReferencedAssemblyByFullName(module, assemblyNameReference, specContext).LoadModuleSpec(module);
            }
            if (specContext == null)
            {
                specContext ??= new SpecContext(AssemblyLocator.GetLocator(module));
            }
            return LoadReferencedModule(specContext, assemblyNameReference);
        }

        private ModuleSpec LoadReferencedModule(ISpecContext specContext, AssemblyNameReference assemblyReference)
        {
            return LoadReferencedAssembly(specContext, assemblyReference).LoadModuleSpec(assemblyReference);
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

        public TypeSpec GetNullTypeSpec(ISpecContext specContext)
        {
            return _nullTypeSpec ??= new NullTypeSpec(this, specContext);
        }

        public TypeSpec[] TypeSpecs => ModuleSpecs.SelectMany(m => m.TypeSpecs).ToArray();

        public TypeSpec LoadTypeSpec(TypeReference typeReference, ISpecContext specContext)
        {
            if (typeReference == null)
            {
                return GetNullTypeSpec(specContext);
            }
            ModuleSpec module = LoadModuleSpecForTypeReference(typeReference, specContext);
            return module.LoadTypeSpec(typeReference);
        }

        public IEnumerable<TypeSpec> LoadTypeSpecs(IEnumerable<TypeReference> types, ISpecContext specContext)
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference, specContext);
            }
        }

        public IEnumerable<TSpec> LoadTypeSpecs<TSpec>(IEnumerable<TypeReference> types, ISpecContext specContext) 
            where TSpec : TypeSpec
        {
            foreach (var typeReference in types)
            {
                yield return LoadTypeSpec(typeReference, specContext) as TSpec;
            }
        }

        #endregion

        #region Method Specs

        public MethodSpec[] MethodSpecs => TypeSpecs.SelectMany(t => t.Methods).ToArray();

        public MethodSpec LoadMethodSpec(MethodDefinition method, bool allowNull, ISpecContext specContext)
        {
            if (method == null)
            {
                if (!allowNull)
                {
                    AddFault(FaultSeverity.Error, "No MethodSpec for null MethodDefintion");
                }
                return null;
            }
            return LoadTypeSpec(method.DeclaringType, specContext).LoadMethodSpec(method);
        }

        public MethodSpec LoadMethodSpec(MethodReference method, bool allowNull, ISpecContext specContext)
        {
            if (method == null)
            {
                AddFault(allowNull ? FaultSeverity.Debug : FaultSeverity.Warning, "No MethodSpec for null MethodDefintion");
                return null;
            }
            return LoadTypeSpec(method.DeclaringType, specContext).LoadMethodSpec(method);
        }

        public IEnumerable<MethodSpec> LoadSpecsForMethodReferences(IEnumerable<MethodReference> methodReferences, ISpecContext specContext)
        {
            foreach (var methodReference in methodReferences)
            {
                if (methodReference is MethodDefinition methodDefinition)
                {
                    yield return LoadMethodSpec(methodDefinition, true, specContext);
                }
                else if (methodReference.DeclaringType?.Scope != null)
                {
                    yield return LoadMethodSpec(methodReference, true, specContext);
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
                    yield return LoadMethodSpec(methodDefinition, true, specContext);
                }
            }            
        }

        #endregion

        #region Property Specs

        public PropertySpec[] PropertySpecs => TypeSpecs.SelectMany(t => t.Properties).ToArray();

        public PropertySpec LoadPropertySpec(PropertyReference propertyReference, bool allowNull, ISpecContext specContext)
        {
            if (propertyReference == null)
            {
                AddFault(allowNull ? FaultSeverity.Debug : FaultSeverity.Warning, "No PropertySpec for null PropertyDefinition");
                return null;
            }
            return LoadTypeSpec(propertyReference.DeclaringType, specContext).LoadPropertySpec(propertyReference.Resolve());
        }

        public IEnumerable<PropertySpec> LoadPropertySpecs(IEnumerable<PropertyReference> propertyReferences, ISpecContext specContext)
        {
            foreach (var propertyReference in propertyReferences)
            {
                yield return LoadPropertySpec(propertyReference, true, specContext);
            }
        }

        #endregion

        #region Field Specs

        public FieldSpec[] FieldSpecs => TypeSpecs.SelectMany(t => t.Fields).ToArray();

        public FieldSpec LoadFieldSpec(FieldReference fieldReference, bool allowNull, ISpecContext specContext)
        {
            if (fieldReference == null)
            {
                if (!allowNull)
                {
                    AddFault(FaultSeverity.Error, "No PropertySpec for null PropertyDefinition");
                }
                return null;
            }
            return LoadTypeSpec(fieldReference.DeclaringType, specContext).LoadFieldSpec(fieldReference.Resolve());
        }

        #endregion

        #region Event Specs

        public EventSpec[] EventSpecs => TypeSpecs.SelectMany(t => t.Events).ToArray();

        #endregion

        #region Attribute Specs

        public IReadOnlyDictionary<string, TypeSpec> Attributes => _attributeSpecs;

        ConcurrentDictionary<string, TypeSpec> _attributeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public TypeSpec[] TryLoadAttributeSpecs(Func<CustomAttribute[]> getAttributes, AbstractSpec decoratedSpec, ISpecContext specContext)
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
            return LoadAttributeSpecs(attributes, decoratedSpec, specContext);
        }

        public TypeSpec[] LoadAttributeSpecs(CustomAttribute[] attibutes, AbstractSpec decoratedSpec, ISpecContext specContext)
        {
            return attibutes.Select(f => LoadAttributeSpec(f, decoratedSpec, specContext)).ToArray();
        }

        private TypeSpec LoadAttributeSpec(CustomAttribute attribute, AbstractSpec decoratedSpec, ISpecContext specContext)
        {
            var attributeSpec = LoadTypeSpec(attribute.AttributeType, specContext);
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

        public ISpecDependency RegisterOperandDependency(object operand, MethodSpec methodSpec, ISpecContext specContext)
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
                TypeReference typeReference => new MethodToTypeDependency(LoadTypeSpec(typeReference, specContext), methodSpec),
                MethodReference methodReference => new MethodToMethodDependency(LoadMethodSpec(methodReference, false, specContext), methodSpec),
                PropertyReference propertyReference => new MethodToPropertyDependency(LoadPropertySpec(propertyReference, false, specContext), methodSpec),
                FieldReference fieldReference => new MethodToFieldDependency(LoadFieldSpec(fieldReference, false, specContext), methodSpec),
                //ParameterReference parameterReference => parameterReference.ParameterType.Module,
                //VariableReference variableReference => variableReference.VariableType.Module,
                //Instruction operandInstruction => operandInstruction.Operand,
                //Instruction[] operandInstructions => operandInstructions.Select(t => t.Operand),
                _ => null
            };
            
        }
    }
}
