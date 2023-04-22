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
            Adder = _specManager.LoadMethodSpec(_adder, true, DeclaringType.Module.AssemblyLocator);
            Remover = _specManager.LoadMethodSpec(_remover, true, DeclaringType.Module.AssemblyLocator);
            EventType = _specManager.LoadTypeSpec(_eventDefinition.EventType, DeclaringType.Module.AssemblyLocator);
            EventType.RegisterAsDelegateFor(this);            
            _attributes = _specManager.TryLoadAttributeSpecs(GetAttributes, this, DeclaringType.Module.AssemblyLocator);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return _eventDefinition.CustomAttributes.ToArray();
        }

        protected override TypeSpec[] TryLoadAttributeSpecs()
        {
            return _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this, DeclaringType.Module.AssemblyLocator);
        }

        public override string ToString()
        {
            return _eventDefinition.Name;
        }
    }
}
