using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class PropertySpec : AbstractSpec
    {
        private PropertyInfo _propertyInfo;
        private MethodInfo _getter;
        private MethodInfo _setter;

        public PropertySpec(PropertyInfo propertyInfo, List<IRule> rules) : base(rules)
        {
            _propertyInfo = propertyInfo;
            _getter = propertyInfo.GetGetMethod();
            _setter = propertyInfo.GetSetMethod();
        }

        public MethodSpec Getter { get; private set; }
        public MethodSpec Setter { get; private set; }
        public TypeSpec PropertyType { get; private set; }
        public TypeSpec DeclaringType { get; private set; }

        public IEnumerable<MethodInfo> InnerMethods()
        {
            return new[] { _getter, _setter };
        }

        protected override void BeginProcessing(Analyser analyser, ISpecManager specManager)
        {
            Getter = specManager.LoadMethodSpec(_getter);
            Setter = specManager.LoadMethodSpec(_setter);
            PropertyType = specManager.TryLoadTypeSpec(() => _propertyInfo.PropertyType);
            DeclaringType = specManager.TryLoadTypeSpec(() => _propertyInfo.DeclaringType);            
        }

        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task getter = Getter?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task setter = Setter?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task propertyType = PropertyType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            Task declaringType = DeclaringType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(getter, setter, propertyType, declaringType);
        }

        public override string ToString()
        {
            return _propertyInfo.Name;
        }
    }
}
