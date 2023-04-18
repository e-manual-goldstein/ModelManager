using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public abstract class AbstractMemberSpec<TMemberSpec> : AbstractSpec, IMemberSpec, IImplementsSpec<TMemberSpec>
        where TMemberSpec : AbstractSpec
    {
        protected AbstractMemberSpec(TypeSpec declaringType, ISpecManager specManager) : base(specManager)
        {
            _declaringType = declaringType;
        }

        TypeSpec _declaringType;
        public TypeSpec DeclaringType => _declaringType ??= TryGetDeclaringType();

        public abstract TypeSpec ResultType { get; }

        public string ExplicitName { get; protected set; }

        List<TMemberSpec> _implementationFor = new();
        public TMemberSpec[] ImplementationFor => _implementationFor.ToArray();

        public void RegisterAsImplementation(TMemberSpec implementedSpec)
        {
            if (!_implementationFor.Contains(implementedSpec))
            {
                _implementationFor.Add(implementedSpec);
            }
        }

        protected abstract TypeSpec TryGetDeclaringType();


        #region Parameters

        ConcurrentDictionary<string, ParameterSpec> _parameterSpecs = new ConcurrentDictionary<string, ParameterSpec>();

        private ParameterSpec LoadParameterSpec(ParameterDefinition parameterDefinition)
        {
            return _parameterSpecs.GetOrAdd(parameterDefinition.Name, CreateParameterSpec(parameterDefinition));
        }

        private ParameterSpec CreateParameterSpec(ParameterDefinition parameterDefinition)
        {
            //var typeSpecs = LoadTypeSpecs(parameterDefinition.CustomAttributes.Select(t => t.AttributeType));
            return new ParameterSpec(parameterDefinition, this, _specManager);
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
