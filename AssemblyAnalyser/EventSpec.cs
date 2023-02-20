using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class EventSpec : AbstractSpec, IMemberSpec
    {
        private EventInfo _eventInfo;
        private MethodInfo _adder;
        private MethodInfo _remover;

        public EventSpec(EventInfo eventInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _eventInfo = eventInfo;
            _adder = eventInfo.GetAddMethod();
            _remover = eventInfo.GetRemoveMethod();
            DeclaringType = declaringType;
            IsSystemEvent = declaringType.IsSystemType;
        }

        public MethodSpec Adder { get; private set; }
        public MethodSpec Remover { get; private set; }

        public IEnumerable<MethodInfo> InnerMethods()
        {
            return new[] { _adder, _remover };
        }

        public IEnumerable<MethodSpec> InnerSpecs()
        {
            return new[] { Adder, Remover }.Where(c => c != null);
        }


        public string EventName => _eventInfo.Name;
        public TypeSpec EventType { get; private set; }
        public bool IsSystemEvent { get; set; }
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => EventType;

        protected override void BuildSpec()
        {
            Adder = _specManager.LoadMethodSpec(_adder, DeclaringType);
            Remover = _specManager.LoadMethodSpec(_remover, DeclaringType);
            if (_specManager.TryLoadTypeSpec(() => _eventInfo.EventHandlerType, out TypeSpec typeSpec))
            {
                EventType = typeSpec;
                EventType.RegisterAsResultType(this);
            }
            Attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        private CustomAttributeData[] GetAttributes()
        {
            return _eventInfo.GetCustomAttributesData().ToArray();
        }


        protected override async Task BeginAnalysis(Analyser analyser)
        {
            Task fieldType = EventType?.AnalyseAsync(analyser) ?? Task.CompletedTask;
            await Task.WhenAll(fieldType);
        }

        public override string ToString()
        {
            return _eventInfo.Name;
        }
    }
}
