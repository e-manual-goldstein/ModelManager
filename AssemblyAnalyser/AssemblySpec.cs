using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class AssemblySpec : ISpec
    {
        public static AssemblySpec NullSpec = CreateNullSpec();

        private static AssemblySpec CreateNullSpec()
        {
            var spec = new AssemblySpec("null");
            //spec.ExclusionRules.Add(new ExclusionRule<TypeSpec>(spec => true));
            return spec;
        }

        Assembly _assembly;
        private bool _analysing;
        private bool _analysed;
        private bool _analysable;

        public AssemblySpec(Assembly assembly) : this(assembly.FullName)
        {
            _assembly = assembly;
            AssemblyShortName = _assembly.GetName().Name;
            _analysable = true;
        }

        public AssemblySpec(string fullName)
        {
            AssemblyFullName = fullName;
            ExclusionRules = new List<ExclusionRule<AssemblySpec>>();
            InclusionRules = new List<InclusionRule<AssemblySpec>>();
        }


        public string AssemblyFullName { get; }
        public string AssemblyShortName { get; }

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
            if (Included() && !Excluded())
            {
                var typeTasks = Task.WhenAll(_typeSpecs.Select(t => t.AnalyseAsync(analyser)));
                var assemblyTasks = Task.WhenAll(_referencedAssemblies.Select(a => a.AnalyseAsync(analyser)));

                await Task.WhenAll(typeTasks, assemblyTasks);
            }
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
            return types.Select(t => analyser.TryLoadTypeSpec(() => t)).ToArray();
        }

        public override string ToString()
        {
            return AssemblyFullName;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<AssemblySpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<AssemblySpec>> InclusionRules { get; private set; }
    }
}
