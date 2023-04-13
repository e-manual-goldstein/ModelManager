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

        public EventSpec(EventDefinition eventInfo, TypeSpec declaringType, ISpecManager specManager) 
            : base(specManager)
        {
            _eventDefinition = eventInfo;
            _adder = eventInfo.AddMethod;
            _remover = eventInfo.RemoveMethod;
            DeclaringType = declaringType;
            IsSystem = declaringType.IsSystem;
        }

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
        public bool? IsSystemEvent { get; set; }
        public TypeSpec DeclaringType { get; }

        TypeSpec IMemberSpec.ResultType => EventType;

        protected override void BuildSpec()
        {
            Adder = _specManager.LoadMethodSpec(_adder);
            Remover = _specManager.LoadMethodSpec(_remover);
            EventType = _specManager.LoadTypeSpec(_eventDefinition.EventType);
            EventType.RegisterAsDelegateFor(this);            
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _eventDefinition.CustomAttributes.ToArray();
        }
                
        public override string ToString()
        {
            return _eventDefinition.Name;
        }
    }
}
