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


        public string AssemblyName { get; }

        TypeSpec[] _typeSpecs;
        public TypeSpec[] TypeSpecs => _typeSpecs;

        public async Task AnalyseAsync(Analyser analyser)
        {
            _typeSpecs = LoadTypeSpecs(analyser);
            await Task.WhenAll(_typeSpecs.Select(t => t.AnalyseAsync(analyser)).ToArray());
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
    }
}
