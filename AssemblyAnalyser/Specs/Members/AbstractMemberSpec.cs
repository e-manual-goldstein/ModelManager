using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public abstract class AbstractMemberSpec<TMemberSpec> : AbstractSpec, 
        IAbstractMemberSpec, IImplementsSpec<TMemberSpec>
        where TMemberSpec : AbstractSpec, IAbstractMemberSpec
    {
        protected AbstractMemberSpec(TypeSpec declaringType, ISpecManager specManager, ISpecContext specContext) : base(specManager, specContext)
        {
            _declaringType = declaringType;
        }

        TypeSpec _declaringType;
        public TypeSpec DeclaringType => _declaringType ??= TryGetDeclaringType();

        public abstract TypeSpec ResultType { get; }

        public string ExplicitName { get; protected set; }
        public bool IsOverride { get; protected set; }
        public bool IsHideBySig { get; protected set; }

        List<TMemberSpec> _implementationFor = new();
        public virtual TMemberSpec[] ImplementationFor => _implementationFor.ToArray();

        public void RegisterAsImplementation(TMemberSpec implementedSpec)
        {
            if (DeclaringType.IsInterface)
            {
                _specManager.AddFault(this, FaultSeverity.Critical, "Interface Member cannot be registered as an Implementation");
            }
            _implementationFor.Add(implementedSpec);
            implementedSpec.RegisterImplementation(this);
        }

        List<IMemberSpec> _implementations = new();
        public virtual IMemberSpec[] Implementations => _implementations.ToArray();

        public void RegisterImplementation(IMemberSpec implementingMemberSpec)
        {
            if (!DeclaringType.IsInterface)
            {
                _specManager.AddFault(this, FaultSeverity.Critical, "Interface Member cannot be registered as an Implementation");
            }
            _implementations.Add(implementingMemberSpec);
        }

        protected abstract TypeSpec TryGetDeclaringType();

        protected override void BuildSpec()
        {
            
        }

        #region Parameters

        ConcurrentDictionary<string, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<string, ParameterSpec>();

        private ParameterSpec LoadParameterSpec(ParameterDefinition parameterDefinition)
        {
            return _parameterSpecs.GetOrAdd(parameterDefinition.Name, CreateParameterSpec(parameterDefinition));
        }

        private ParameterSpec CreateParameterSpec(ParameterDefinition parameterDefinition)
        {
            //var typeSpecs = LoadTypeSpecs(parameterDefinition.CustomAttributes.Select(t => t.AttributeType));
            return new ParameterSpec(parameterDefinition, this, _specManager, _specContext);
        }

        public ParameterSpec[] LoadParameterSpecs(ParameterDefinition[] parameterDefinitions)
        {
            return parameterDefinitions.Select(p => LoadParameterSpec(p)).ToArray();
        }

        public ParameterSpec[] TryLoadParameterSpecs(Func<ParameterDefinition[]> parameterDefinitions)
        {
            ParameterDefinition[] parameterInfos = null;
            try
            {
                parameterInfos = parameterDefinitions();
            }
            finally
            {
                parameterInfos ??= Array.Empty<ParameterDefinition>();
            }
            return LoadParameterSpecs(parameterInfos);
        }

        #endregion
    }
}
