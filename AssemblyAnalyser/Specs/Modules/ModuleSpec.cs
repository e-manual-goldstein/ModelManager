using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AssemblyAnalyser.Extensions;
using AssemblyAnalyser.Specs;
using Mono.Cecil;
namespace AssemblyAnalyser
{
    public class ModuleSpec : AbstractSpec
    {
        AssemblySpec _assembly;
        //the _baseVersion is the version of the module for which an assembly has been located
        //this is the version which corresponds to the file found at FilePath
        protected ModuleDefinition _baseVersion;
        
        #region Constructors
        public ModuleSpec(ModuleDefinition module, string filePath, AssemblySpec assemblySpec, ISpecManager specManager) 
            : this(module.Assembly.FullName, assemblySpec, specManager)
        {
            Versions = new() { { module.Assembly.FullName, module.Assembly.Name } };
            _baseVersion = module;            
            ModuleShortName = module.Assembly.Name.Name;
            FilePath = filePath;
            IsSystem = _assembly.IsSystem;
        }

        protected ModuleSpec(string assemblyFullName, AssemblySpec assemblySpec, ISpecManager specManager)
            : base(specManager)
        {
            _assembly = assemblySpec;
            ModuleFullName = assemblyFullName;
        }
        #endregion

        #region Properties
        public ModuleDefinition Definition => _baseVersion;

        public string ModuleShortName { get; protected set; }

        public string FilePath { get; }

        public string ModuleFullName { get; }

        protected Dictionary<string, AssemblyNameReference> Versions { get; set; }

        public IAssemblyLocator AssemblyLocator => _assembly.AssemblyLocator;

        #endregion

        #region Referenced Modules
        ModuleSpec[] _referencedModules;

        public ModuleSpec[] ReferencedModules => _referencedModules ??= LoadReferencedModules();

        public ModuleSpec[] LoadReferencedModules(bool includeSystem = false)
        {
            return (_referencedModules ??= _specManager.LoadReferencedModules(_baseVersion, _assembly.AssemblyLocator).ToArray())
                .Where(r => !r.IsSystem || includeSystem).ToArray();
        }
        #endregion

        #region Referenced Assemblies
        AssemblySpec[] _referencedAssemblies;

        public AssemblySpec[] ReferencedAssemblies => _referencedAssemblies ??= LoadReferencedAssemblies();

        public AssemblySpec[] LoadReferencedAssemblies(bool includeSystem = false)
        {
            return (_referencedAssemblies ??= _specManager.TryLoadReferencedAssemblies(_baseVersion, AssemblyLocator).ToArray())
                .Where(r => !r.IsSystem || includeSystem).ToArray();
        }
        #endregion

        #region Type Specs

        public TypeSpec[] TypeSpecs => _typeSpecs.Values.ToArray();

        protected ConcurrentDictionary<string, TypeSpec> _typeSpecs = new ConcurrentDictionary<string, TypeSpec>();

        public TypeSpec[] TryLoadTypesForModule(ModuleDefinition module)
        {
            var specs = new List<TypeSpec>();
            var types = module.GetTypes();
            TryLoadTypeSpecs(() => types.ToArray(), out TypeSpec[] typeSpecs);

            return typeSpecs;
        }

        public virtual TypeSpec LoadTypeSpec(TypeReference type)
        {
            if (type == null)
            {
                return _specManager.GetNullTypeSpec();
            }
            return LoadFullTypeSpec(type);
        }

        protected TypeSpec LoadFullTypeSpec(TypeReference type)
        {
            bool typeReferenceIsArray = type.IsArray; //Resolving TypeReference to TypeDefinition causes loss of IsArray definition
            var uniqueTypeName = type.CreateUniqueTypeSpecName(typeReferenceIsArray);
            if (type.IsGenericParameter && type is GenericParameter genericParameter)
            {
                return _typeSpecs.GetOrAdd(uniqueTypeName, (key) => CreateGenericParameterSpec(genericParameter));
            }
            if (type.HasGenericParameters && type is TypeDefinition genericTypeDefinition)
            {
                return _typeSpecs.GetOrAdd(uniqueTypeName, (key) => CreateGenericTypeSpec(genericTypeDefinition));
            }
            if (type.IsGenericInstance && type is GenericInstanceType genericInstanceType)
            {
                return _typeSpecs.GetOrAdd(uniqueTypeName, (key) => CreateGenericInstanceSpec(genericInstanceType, key));
            }
            if (type.IsArray && type is ArrayType arrayType)
            {
                return _typeSpecs.GetOrAdd(uniqueTypeName, (key) => CreateArrayTypeSpec(arrayType, key));
            }
            return _typeSpecs.GetOrAdd(uniqueTypeName, (key) => CreateFullTypeSpec(type));
        }

        private TypeSpec CreateFullTypeSpec(TypeReference type)
        {
            TryGetTypeDefinition(ref type); //Try this only ONCE per TypeReference
            if (type is TypeDefinition typeDefinition)
            {
                var spec = new TypeSpec(typeDefinition, this, _specManager);
                return spec;
            }
            return new MissingTypeSpec($"{type.Namespace}.{type.Name}", type.FullName, this, _specManager);
        }

        private TypeSpec CreateGenericParameterSpec(GenericParameter type)
        {
            var spec = new GenericParameterSpec(type, this, _specManager);
            return spec;
        }

        private TypeSpec CreateGenericTypeSpec(TypeDefinition typeDefinition)
        {
            var spec = new GenericTypeSpec(typeDefinition, this, _specManager);
            return spec;
        }

        private TypeSpec CreateGenericInstanceSpec(GenericInstanceType type, string fullTypeName)
        {
            var spec = new GenericInstanceSpec(type, fullTypeName, this, _specManager);
            return spec;
        }

        private TypeSpec CreateArrayTypeSpec(ArrayType type, string fullTypeName)
        {
            var elementSpec = LoadTypeSpec(type.ElementType);
            var spec = new ArrayTypeSpec(type, elementSpec, this, _specManager);
            return spec;
        }

        private void TryGetTypeDefinition(ref TypeReference type)
        {
            TypeDefinition typeDefinition = null;
            if (type.IsGenericInstance)
            {
                throw new ArgumentException("Cannot have TypeDefinition for Generic Instance");
            }
            if (typeDefinition == null)
            {
                
                var scopeName = type.Scope.GetScopeNameWithoutExtension();
                
                if (scopeName == _baseVersion.GetScopeNameWithoutExtension())
                {
                    if (type.IsGenericParameter)
                    {
                        var genericParameter = GetGenericParameter(type);
                        if (genericParameter != null)
                        {
                            type = genericParameter;
                            return;
                        }
                    }
                    else
                    {
                        typeDefinition = GetTypeDefinition(type) ?? GetTypeDefinition(type.GetElementType());
                    }
                }
            }
            typeDefinition ??= TryResolveTypeDefinition(type);
            if (typeDefinition != null)
            {
                type = typeDefinition;
                return;
            }
            _specManager.AddFault(this, FaultSeverity.Warning, $"Could not fully resolve TypeDefinition {type}");
        }

        private TypeDefinition TryResolveTypeDefinition(TypeReference type)
        {
            ModuleDefinition moduleDefinition = null;
            try
            {
                if ((moduleDefinition = type.Module) != null)
                {
                    if (moduleDefinition.AssemblyResolver != null)
                    {
                        
                    }
                    return type.Resolve();
                }
            }
            catch (AssemblyResolutionException assemblyResolutionException)
            {
                _specManager.AddFault($"Failed to resolve TypeDefinition {type}: {assemblyResolutionException.Message}");
            }
            return null;
        }

        public bool TryLoadTypeSpec(Func<TypeReference> getType, out TypeSpec typeSpec)
        {
            return TryLoadTypeSpec(getType(), out typeSpec);
        }

        public bool TryLoadTypeSpec(TypeReference typeReference, out TypeSpec typeSpec)
        {
            typeSpec = LoadTypeSpec(typeReference);
            return typeSpec != null && !typeSpec.IsNullSpec;
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
                _specManager.AddFault(this, FaultSeverity.Error, ex.Message);
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

        protected override CustomAttribute[] GetAttributes()
        {
            return _baseVersion.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, AssemblyLocator);
        }

        protected override void BuildSpec()
        {
            foreach (var referencedModule in ReferencedModules)
            {
                referencedModule.Process();
                referencedModule.RegisterAsReferencedAssemblyFor(this);
            }
            foreach (var typeDefinition in _baseVersion.Types) 
            {
                LoadTypeSpec(typeDefinition);
            }
        }

        public void AddModuleVersion(AssemblyNameReference reference)
        {
            if (!Versions.ContainsKey(reference.Version.ToString()))
            {
                Versions[reference.Version.ToString()] = reference;
            }
        }

        public override string ToString()
        {
            return ModuleFullName;
        }

        public TypeDefinition GetTypeDefinition(TypeReference typeReference)
        {
            var types = _baseVersion.Types.Where(t => t.FullName == typeReference.FullName).ToArray();
            if (types.Any())
            {
                if (types.Length > 1)
                {

                }
                else
                {
                    return types.Single();
                }
            }
            return _baseVersion.GetType(typeReference.FullName);
        }

        public GenericParameter GetGenericParameter(TypeReference typeReference)
        {
            var typesWithGenericParameters = _baseVersion.Types.Where(t => t.HasGenericParameters).ToArray();
            if (typeReference.DeclaringType != null)
            {
                var matchingType = typesWithGenericParameters.SingleOrDefault(t => t.FullName == typeReference.DeclaringType.FullName);
                return matchingType.GenericParameters.SingleOrDefault(t => t.FullName == typeReference.FullName);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public TypeSpec GetTypeSpec(string fullTypeName)
        {
            var matchingByName = TypeSpecs.Where(t => t.FullTypeName == fullTypeName).ToList();
            return matchingByName.SingleOrDefault();
        }

        #region Post Build

        #region Generic Type Implementations
        List<GenericInstanceSpec> _genericTypeImplementations = new List<GenericInstanceSpec>();
        public GenericInstanceSpec[] GenericTypeImplementations => _genericTypeImplementations.ToArray();

        public void AddGenericTypeImplementation(GenericInstanceSpec genericInstance)
        {
            if (TypeSpecs.Contains(genericInstance))
            {

            }
            if (!_genericTypeImplementations.Contains(genericInstance))
            {
                _genericTypeImplementations.Add(genericInstance);
            }
        }
        #endregion

        #region Type Dependencies
        List<TypeSpec> _dependentTypes = new List<TypeSpec>();
        public TypeSpec[] DependentTypes => _dependentTypes.ToArray();

        public void RegisterDependentType(TypeSpec typeSpec)
        {
            if (!_dependentTypes.Contains(typeSpec))
            {
                _dependentTypes.Add(typeSpec);
            }
        }
        #endregion

        #region Module References
        List<ModuleSpec> _referencedBy = new List<ModuleSpec>();

        public ModuleSpec[] ReferencedBy => _referencedBy.ToArray();

        private void RegisterAsReferencedAssemblyFor(ModuleSpec assemblySpec)
        {
            if (!_referencedBy.Contains(assemblySpec))
            {
                _referencedBy.Add(assemblySpec);
            }
        } 
        #endregion

        #endregion

    }
}
