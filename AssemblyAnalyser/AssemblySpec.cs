using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class AssemblySpec : AbstractSpec
    {
        public static AssemblySpec NullSpec = CreateNullSpec();

        private static AssemblySpec CreateNullSpec()
        {
            var spec = new AssemblySpec("null", null, new List<IRule>());
            spec.Exclude("Null Spec");
            spec.SkipProcessing("Null Spec");
            return spec;
        }

        //Assembly _assembly;

        public AssemblySpec(string assemblyFullName, string shortName, string filePath,
            ISpecManager specManager, List<IRule> rules) : this(assemblyFullName, specManager, rules)
        {
            //_assembly = assembly;
            AssemblyShortName = shortName;
            FilePath = filePath;
            IsSystemAssembly = AssemblyLoader.IsSystemAssembly(filePath);
        }

        AssemblySpec(string assemblyFullName, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _representedAssemblyNames.Add(assemblyFullName);
            _specManager = specManager;
            AssemblyFullName = assemblyFullName;            
        }

        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; }
        public string FilePath { get; internal set; }

        public string TargetFrameworkVersion { get; internal set; }
        public string ImageRuntimeVersion { get; internal set; }

        List<string> _representedAssemblyNames = new List<string>();

        public void AddRepresentedName(string assemblyFullName)
        {
            if (!_representedAssemblyNames.Any(n => n == assemblyFullName))
            {
                _representedAssemblyNames.Add(assemblyFullName);
            }
        }

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs;

        AssemblySpec[] _referencedAssemblies;
        
        public bool IsSystemAssembly { get; }

        public AssemblySpec[] LoadReferencedAssemblies(bool includeSystem = true)
        {
            return (_referencedAssemblies ??= _specManager.LoadReferencedAssemblies(AssemblyFullName, FilePath, TargetFrameworkVersion, ImageRuntimeVersion))
                .Where(r => !r.IsSystemAssembly || includeSystem).ToArray();
        }

        List<AssemblySpec> _referencedBy = new List<AssemblySpec>();

        public AssemblySpec[] ReferencedBy => _referencedBy.ToArray();

        private void RegisterAsReferencedAssemblyFor(AssemblySpec assemblySpec)
        {
            if (!_referencedBy.Contains(assemblySpec))
            {
                _referencedBy.Add(assemblySpec);
            }
        }

        protected override void BuildSpec()
        {            
            LoadReferencedAssemblies();
            foreach (var referencedAssembly in _referencedAssemblies)
            {
                referencedAssembly.RegisterAsReferencedAssemblyFor(this);
            }
            _typeSpecs = _specManager.TryLoadTypesForAssembly(this);
            Attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        List<TypeSpec> _dependentTypes = new List<TypeSpec>();

        public TypeSpec[] DependentTypes => _dependentTypes.ToArray(); 

        public void RegisterDependentType(TypeSpec typeSpec)
        {
            if (!_dependentTypes.Contains(typeSpec))
            {
                _dependentTypes.Add(typeSpec);
            }
        }
        
        private CustomAttributeData[] GetAttributes()
        {
            var loader = AssemblyLoader.GetLoader(TargetFrameworkVersion, ImageRuntimeVersion);
            var assembly = loader.LoadAssemblyByPath(FilePath);
            return assembly.GetCustomAttributesData().ToArray();
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            var typeTasks = Task.WhenAll(GetTypeTasks(analyser));
            var assemblyTasks = Task.WhenAll(GetAssemblyTasks(analyser));
            await Task.WhenAll(typeTasks, assemblyTasks);                        
        }

        private IEnumerable<Task> GetTypeTasks(Analyser analyser)
        {
            return _typeSpecs.Select(async t =>
            {
                await t.AnalyseAsync(analyser);
                UpdateProgress();
            });
        }

        private IEnumerable<Task> GetAssemblyTasks(Analyser analyser)
        {
            return _referencedAssemblies.Select(async a =>
            {
                await a.AnalyseAsync(analyser);
                UpdateProgress();                
            });
        }

        public override string ToString()
        {
            return AssemblyFullName;
        }

        public bool MatchesName(string assemblyName)
        {
            return AssemblyShortName == assemblyName || AssemblyFullName == assemblyName;
        }

        double _assemblyProgress = 0f;
        double _typesProgress = 0f;
        private void UpdateProgress()
        {
            _assemblyProgress = 100.0 * _referencedAssemblies.Count(d => d.Analysed) / _referencedAssemblies.Length;
            _typesProgress = 100.0 * _typeSpecs.Count(d => d.Analysed) / _typeSpecs.Length;
            if (_typesProgress % 1 == 0 || _assemblyProgress % 1 == 0)
            {
                //Logger.Log(LogLevel.Information, $"Types Progress: {_typesProgress}%\tAssembly Progress: {_assemblyProgress}");
            }
        }

        public IEnumerable<string> InterfaceReport()
        {
            foreach (var @interface in TypeSpecs.Where(t => t.IsInterface))
            {
                foreach (var assembly in @interface.GetDependentAssemblies())
                {
                    yield return $"{@interface}\t{assembly.AssemblyShortName}";
                }
            }
        }

        public IEnumerable<string> GenericTypeReport()
        {
            foreach (var typeSpec in TypeSpecs.Where(t => t.IsGenericType != t.IsGenericTypeDefinition))
            {                
                yield return $"{this}\t{typeSpec}";                
            }
        }
    }
}
