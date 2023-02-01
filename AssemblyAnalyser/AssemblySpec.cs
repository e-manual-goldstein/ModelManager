using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
            var spec = new AssemblySpec("null", new List<IRule>());
            spec.Exclude("Null Spec");
            spec.SkipProcessing("Null Spec");
            return spec;
        }

        Assembly _assembly;

        public AssemblySpec(Assembly assembly, ISpecManager specManager, List<IRule> rules) : this(assembly.FullName, rules)
        {
            _assembly = assembly;
            _specManager = specManager;
            _representedAssemblyNames.Add(_assembly.GetName());
            AssemblyShortName = _assembly.GetName().Name;
            FilePath = _assembly.Location;
        }

        public AssemblySpec(string fullName, List<IRule> rules) : base(rules)
        {
            AssemblyFullName = fullName;            
        }

        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; }
        public string FilePath { get; internal set; }

        List<AssemblyName> _representedAssemblyNames = new List<AssemblyName>();

        public void AddRepresentedName(AssemblyName assemblyName)
        {
            if (!_representedAssemblyNames.Any(n => n.FullName == assemblyName.FullName))
            {
                _representedAssemblyNames.Add(assemblyName);
            }
        }

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs;

        AssemblySpec[] _referencedAssemblies;
        public AssemblySpec[] ReferencedAssemblies => _referencedAssemblies;

        public AssemblySpec[] LoadReferencedAssemblies()
        {
            return _referencedAssemblies ??= _specManager.LoadAssemblySpecs(_assembly.GetReferencedAssemblies().ToArray());
        }

        protected override void BeginProcessing(Analyser analyser, ISpecManager specManager)
        {
            if (_assembly != null)
            {
                _referencedAssemblies = specManager.LoadAssemblySpecs(_assembly.GetReferencedAssemblies().ToArray());
                _typeSpecs = specManager.TryLoadTypeSpecs(() => _assembly.GetTypes());
                Array.ForEach(_typeSpecs, spec => spec.Process(analyser, specManager));
            }
            else
            {
                _referencedAssemblies = Array.Empty<AssemblySpec>();
                _typeSpecs = Array.Empty<TypeSpec>();
            }
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
