using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public class EventSpec : AbstractSpec, IMemberSpec
    {
        private EventDefinition _eventDefinition;
        private MethodDefinition _adder;
        private MethodDefinition _remover;

        public EventSpec(EventDefinition eventInfo, TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext) 
            : base(specManager, specContext)
        {
            _eventDefinition = eventInfo;
            _adder = eventInfo.AddMethod;
            _remover = eventInfo.RemoveMethod;
            DeclaringType = declaringType;
            IsSystem = declaringType.IsSystem;
        }

        public string ExplicitName { get; }
        public EventDefinition Definition => _eventDefinition;

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


        public string EventName => _eventDefinition.Name;
        public TypeSpec EventType { get; private set; }
        
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => EventType;

        protected override void BuildSpec()
        {
            Adder = _specManager.LoadMethodSpec(_adder, true, _specContext);
            Remover = _specManager.LoadMethodSpec(_remover, true, _specContext);
            EventType = _specManager.LoadTypeSpec(_eventDefinition.EventType, _specContext);
            EventType.RegisterAsDelegateFor(this);            
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this, _specContext);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _eventDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, _specContext);
        }

        public override string ToString()
        {
            return _eventDefinition.Name;
        }
    }
}
