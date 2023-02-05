using System;
using System.Collections.Generic;
using System.Linq;

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
        }

        public AssemblySpec(string assemblyFullName, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
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
        public AssemblySpec[] ReferencedAssemblies => _referencedAssemblies;


        public AssemblySpec[] LoadReferencedAssemblies()
        {
            //Assembly assembly = _specManager.ReloadAssembly(AssemblyFullName);
            return _referencedAssemblies ??= _specManager.LoadReferencedAssemblies(AssemblyFullName, FilePath);
        }

        protected override void BuildSpec()
        {            
            LoadReferencedAssemblies();
            _typeSpecs = _specManager.TryLoadTypesForAssembly(this);
            //Array.ForEach(_typeSpecs, spec => spec.Process());           
        }

        //private TypeSpec[] CreateTypeSpecs()
        //{
        //    _specManager.TryLoadTypesForAssembly(AssemblyFullName);
        //}

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

        //private TypeSpec[] LoadTypeSpecs(Analyser analyser)
        //{
        //    Type[] types = Array.Empty<Type>();
        //    try
        //    {
        //        if (analyser.CanAnalyse(_assembly))
        //        {
        //            types = _assembly.GetTypes();
        //        }
        //    }            
        //    catch (ReflectionTypeLoadException ex)
        //    {
        //        Logger.Log(LogLevel.Warning, ex.Message, ex.LoaderExceptions);
        //        types = ex.Types.Where(t => t != null).ToArray();
        //    }            
        //    return types.Select(t => analyser.TryLoadTypeSpec(() => t)).ToArray();
        //}

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

        
    }
}
