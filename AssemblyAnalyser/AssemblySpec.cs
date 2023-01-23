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
        }

        public AssemblySpec(string fullName, List<IRule> rules) : base(rules)
        {
            AssemblyFullName = fullName;            
        }


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
            var typeTasks = Task.WhenAll(_typeSpecs.Select(t => t.AnalyseAsync(analyser)));
            var assemblyTasks = Task.WhenAll(_referencedAssemblies.Select(a => a.AnalyseAsync(analyser)));
            await Task.WhenAll(typeTasks, assemblyTasks);                        
        }

        private TypeSpec[] LoadTypeSpecs(Analyser analyser)
        {
            Type[] types = Array.Empty<Type>();
            try
            {
                types = _assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }
            catch (FileNotFoundException)
            {
                types = Array.Empty<Type>();
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

        
    }
}
