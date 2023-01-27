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
            spec.Exclude();
            spec.SkipProcessing();
            return spec;
        }

        Assembly _assembly;

        public AssemblySpec(Assembly assembly, List<IRule> rules) : this(assembly.FullName, rules)
        {
            _assembly = assembly;
            AssemblyShortName = _assembly.GetName().Name;
            var version = _assembly.GetName().Version;
        }

        public AssemblySpec(string fullName, List<IRule> rules) : base(rules)
        {
            AssemblyFullName = fullName;     
            
        }

        public event LogEvent Log;

        public delegate void LogEvent(LogLevel logLevel, string message, params object?[] args);

        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; }

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs;

        AssemblySpec[] _referencedAssemblies;
        public AssemblySpec[] ReferencedAssemblies => _referencedAssemblies;

        protected override void BeginProcessing(Analyser analyser)
        {
            if (_assembly != null)
            {
                _referencedAssemblies = analyser.LoadAssemblySpecs(_assembly.GetReferencedAssemblies().ToArray());
                _typeSpecs = LoadTypeSpecs(analyser);
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

        private TypeSpec[] LoadTypeSpecs(Analyser analyser)
        {
            Type[] types = Array.Empty<Type>();
            try
            {
                if (analyser.CanAnalyse(_assembly))
                {
                    types = _assembly.GetTypes();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }
            catch (FileNotFoundException)
            {
                types = Array.Empty<Type>();
            }
            catch
            {

            }
            return types.Select(t => analyser.TryLoadTypeSpec(() => t)).ToArray();
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
                Log(LogLevel.Information, $"Types Progress: {_typesProgress}%\tAssembly Progress: {_assemblyProgress}");
            }
        }
    }
}
