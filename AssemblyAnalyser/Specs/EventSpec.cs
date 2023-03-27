using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class EventSpec : AbstractSpec, IMemberSpec
    {
        private EventDefinition _eventInfo;
        private MethodDefinition _adder;
        private MethodDefinition _remover;

        public EventSpec(EventDefinition eventInfo, TypeSpec declaringType, ISpecManager specManager, List<IRule> rules) : base(rules, specManager)
        {
            _eventInfo = eventInfo;
            _adder = eventInfo.AddMethod;
            _remover = eventInfo.RemoveMethod;
            DeclaringType = declaringType;
            IsSystemEvent = declaringType.IsSystemType;
        }

        public MethodSpec Adder { get; private set; }
        public MethodSpec Remover { get; private set; }

        public IEnumerable<MethodDefinition> InnerMethods()
        {
            return new[] { _adder, _remover };
        }

        public IEnumerable<MethodSpec> InnerSpecs()
        {
            return new[] { Adder, Remover }.Where(c => c != null);
        }


        public string EventName => _eventInfo.Name;
        public TypeSpec EventType { get; private set; }
        public bool? IsSystemEvent { get; set; }
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => EventType;

        protected override void BuildSpec()
        {
            Adder = _specManager.LoadMethodSpec(_adder, DeclaringType);
            Remover = _specManager.LoadMethodSpec(_remover, DeclaringType);
            if (_specManager.TryLoadTypeSpec(() => _eventInfo.EventType, out TypeSpec typeSpec))
            {
                EventType = typeSpec;
                EventType.RegisterAsDelegateFor(this);
            }
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _eventInfo.CustomAttributes.ToArray();
        }
                
        public override string ToString()
        {
            return _eventInfo.Name;
        }
    }
}
