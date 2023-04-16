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
        //the _baseVersion is the version of the module for which an assembly has been located
        //this is the version which corresponds to the file found at FilePath
        ModuleDefinition _baseVersion;
        
        #region Constructors
        public ModuleSpec(ModuleDefinition module, string filePath,
             ISpecManager specManager) : this(module.Assembly.FullName, specManager)
        {
            AddSearchDirectory(module, filePath);
            Versions = new();
            _baseVersion = module;
            Versions.Add(module.Assembly.FullName, module.Assembly.Name);
            ModuleShortName = module.Assembly.Name.Name;
            FilePath = filePath;
            IsSystem = AssemblyLocator.IsSystemAssembly(filePath);
        }

        protected ModuleSpec(string assemblyFullName, ISpecManager specManager)
            : base(specManager)
        {
            _specManager = specManager;
            ModuleFullName = assemblyFullName;
        }
        #endregion

        #region Properties
        public ModuleDefinition Definition => _baseVersion;

        public string ModuleShortName { get; protected set; }

        public string FilePath { get; }

        public string ModuleFullName { get; }

        protected Dictionary<string, AssemblyNameReference> Versions { get; set; } 
        #endregion

        #region Referenced Modules
        ModuleSpec[] _referencedModules;

        public ModuleSpec[] ReferencedModules => _referencedModules ??= LoadReferencedModules();

        public ModuleSpec[] LoadReferencedModules(bool includeSystem = false)
        {
            return (_referencedModules ??= _specManager.LoadReferencedModules(_baseVersion))
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

        protected string CreateUniqueTypeSpecName(TypeReference type, bool isArray)
        {
            var suffix = isArray ? "[]" : null;
            if (type is GenericParameter genericParameter)
            {
                if (genericParameter.DeclaringMethod == null)
                {
                    return $"{genericParameter.DeclaringType.FullName}[{type.FullName}]{suffix}";
                }
                else
                {
                    if (genericParameter.DeclaringType != null)
                    {

                    }
                    else
                    {
                        var declaringMethod = genericParameter.DeclaringMethod;
                        return $"{declaringMethod.DeclaringType.FullName}.{declaringMethod.Name}<{type.FullName}>{suffix}";
                    }
                }
            }
            if (type is GenericInstanceType genericInstanceType)
            {
                return CreateGenericArgumentsAggregateName(genericInstanceType);
            }
            return $"{type.FullName}{suffix}";
        }

        private string CreateGenericArgumentsAggregateName(GenericInstanceType genericType)
        {
            var prefix = $"{genericType.Namespace}.{genericType.Name}<";
            var argumentNames = genericType.GenericArguments.Select(g => CreateUniqueTypeSpecName(g, false)).ToArray();
            var argumentString = argumentNames.Aggregate((a, b) => $"{a}, {b}");
            return $"{prefix}{argumentString}>";
        }

        private TypeSpec LoadFullTypeSpec(TypeReference type)
        {
            bool typeReferenceIsArray = type.IsArray; //Resolving TypeReference to TypeDefinition causes loss of IsArray definition
            if (type.IsGenericParameter && type is GenericParameter genericParameter)
            {
                return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type, typeReferenceIsArray), (key) => CreateGenericParameterSpec(genericParameter));
            }
            if (type.HasGenericParameters && type is TypeDefinition genericTypeDefinition)
            {
                return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type, typeReferenceIsArray), (key) => CreateGenericTypeSpec(genericTypeDefinition));
            }
            if (type.IsGenericInstance && type is GenericInstanceType genericInstanceType)
            {
                return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type, typeReferenceIsArray), (key) => CreateGenericInstanceSpec(genericInstanceType, key));
            }
            if (!type.IsDefinition)
            {
                TryGetTypeDefinition(ref type);
            }
            return _typeSpecs.GetOrAdd(CreateUniqueTypeSpecName(type, typeReferenceIsArray), (key) => CreateFullTypeSpec(type));
        }

        private TypeSpec CreateFullTypeSpec(TypeReference type)
        {
            if (type is TypeDefinition typeDefinition)
            {
                var spec = new TypeSpec(typeDefinition, _specManager);
                return spec;
            }
            return new MissingTypeSpec($"{type.Namespace}.{type.Name}", type.FullName, _specManager);
        }

        private TypeSpec CreateGenericParameterSpec(GenericParameter type)
        {
            var spec = new GenericParameterSpec(type, _specManager);
            return spec;
        }

        private TypeSpec CreateGenericTypeSpec(TypeDefinition typeDefinition)
        {
            var spec = new GenericTypeSpec(typeDefinition, _specManager);
            return spec;
        }

        private TypeSpec CreateGenericInstanceSpec(GenericInstanceType type, string fullTypeName)
        {
            var spec = new GenericInstanceSpec(type, fullTypeName, _specManager);
            return spec;
        }

        private void TryGetTypeDefinition(ref TypeReference type)
        {
            TypeDefinition typeDefinition = null;
            if (type.IsGenericInstance)
            {
                throw new ArgumentException("Cannot have TypeDefinition for Generic Instance");
            }
            typeDefinition = TryResolveTypeDefinition(type);
            if (typeDefinition == null)
            {
                
                var scopeName = type.Scope.GetScopeNameWithoutExtension();
                
                if (scopeName == ModuleFullName)
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
                        typeDefinition = GetTypeDefinition(type);
                    }
                }
            }
            if (typeDefinition != null)
            {
                type = typeDefinition;
                return;
            }
            _specManager.AddFault("Could not fully resolve TypeDefinition");
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
                        //moduleDefinition.AssemblyResolver.Resolve(type.Mo);
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
            catch (Exception ex)
            {
                typeSpec = TypeSpec.CreateErrorSpec($"{ex.Message}");
            }
            return success;
        }

        public bool TryLoadTypeSpec(TypeReference typeReference, out TypeSpec typeSpec)
        {
            bool success = false;
            try
            {
                typeSpec = LoadTypeSpec(typeReference);
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

        private void AddSearchDirectory(ModuleDefinition module, string filePath)
        {
            if (module.AssemblyResolver is DefaultAssemblyResolver defaultAssemblyResolver)
            {
                if (!defaultAssemblyResolver.GetSearchDirectories().Contains(Path.GetDirectoryName(filePath)))
                {
                    defaultAssemblyResolver.AddSearchDirectory(Path.GetDirectoryName(filePath));
                }
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
            var types = _baseVersion.Types.ToArray();
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
