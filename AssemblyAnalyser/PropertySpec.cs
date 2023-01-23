using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class PropertySpec : ISpec
    {
        private PropertyInfo _propertyInfo;
        private MethodInfo _getter;
        private MethodInfo _setter;

        private bool _analysing;
        private bool _analysed;

        public PropertySpec(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
            _getter = propertyInfo.GetGetMethod();
            _setter = propertyInfo.GetSetMethod();
            InclusionRules = new List<InclusionRule<PropertySpec>>();
            ExclusionRules = new List<ExclusionRule<PropertySpec>>();
        }

        public MethodSpec Getter { get; private set; }
        public MethodSpec Setter { get; private set; }
        public TypeSpec PropertyType { get; private set; }

        public IEnumerable<MethodInfo> InnerMethods()
        {
            return new[] { _getter, _setter };
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            if (!_analysed && !_analysing)
            {
                _analysing = true;
                Getter = analyser.LoadMethodSpec(_getter);
                Setter = analyser.LoadMethodSpec(_setter);
                PropertyType = analyser.TryLoadTypeSpec(() => _propertyInfo.PropertyType);
                await BeginAnalysis(analyser);
            }
        }

        private async Task BeginAnalysis(Analyser analyser)
        {
            Task getter = Getter?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task setter = Setter?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task propertyType = PropertyType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(getter, setter, propertyType);
            _analysed = true;
        }

        public override string ToString()
        {
            return _propertyInfo.Name;
        }

        public bool Excluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool Included()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public List<ExclusionRule<PropertySpec>> ExclusionRules { get; private set; }
        public List<InclusionRule<PropertySpec>> InclusionRules { get; private set; }
    }
}
