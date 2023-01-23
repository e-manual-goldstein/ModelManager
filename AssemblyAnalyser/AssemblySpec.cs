using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class AssemblySpec : ISpec
    {
        Assembly _assembly;

        public AssemblySpec(Assembly assembly)
        {
            _assembly = assembly;
            AssemblyName = assembly.FullName;
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
                _referencedAssemblies = analyser.LoadAssemblySpecs(_assembly.GetReferencedAssemblies().ToArray());
                _typeSpecs = LoadTypeSpecs(analyser);
                var typeTasks = _typeSpecs.Select(t => t.AnalyseAsync(analyser)).ToArray();
                var assemblyTasks = _referencedAssemblies.Select(a => a.AnalyseAsync(analyser)).ToArray();

                await Task.WhenAll(typeTasks.Concat(assemblyTasks));
            }
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
            return types.Select(t => analyser.LoadTypeSpec(t)).ToArray();
        }

        public override string ToString()
        {
            return AssemblyName;
        }
    }
}
