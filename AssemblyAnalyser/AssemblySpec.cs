using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class AssemblySpec : ISpec
    {
        Assembly _assembly;
        private bool _analysing;
        private bool _analysed;
        private bool _analysable;

        public AssemblySpec(Assembly assembly) : this(assembly.FullName)
        {
            _assembly = assembly;
            _analysable = true;
        }

        public AssemblySpec(string fullName)
        {
            AssemblyName = fullName;
        }


        public string AssemblyName { get; }

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs;

        AssemblySpec[] _referencedAssemblies;
        public AssemblySpec[] ReferencedAssemblies => _referencedAssemblies;

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (_assembly != null)
            {
                if (_analysable && !_analysing && !_analysed)
                {
                    _analysing = true;
                    _referencedAssemblies = analyser.LoadAssemblySpecs(_assembly.GetReferencedAssemblies().ToArray());
                    _typeSpecs = LoadTypeSpecs(analyser);
                    await BeginAnalysis(analyser);
                }
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            var typeTasks = Task.WhenAll(_typeSpecs.Select(t => t.AnalyseAsync(analyser)));
            var assemblyTasks = Task.WhenAll(_referencedAssemblies.Select(a => a.AnalyseAsync(analyser)));

            await Task.WhenAll(typeTasks, assemblyTasks);
            _analysed = true;
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
            return types.Select(t => analyser.LoadTypeSpec(t)).ToArray();
        }

        public override string ToString()
        {
            return AssemblyName;
        }
    }
}
