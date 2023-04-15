using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    public class GenericMethodSpec : MethodSpec, IHasGenericParameters
    {
        public GenericMethodSpec(MethodDefinition methodDefinition, ISpecManager specManager) : base(methodDefinition, specManager)
        {
            ExplicitName = CreateGenericMethodName(methodDefinition);
        }

        GenericParameterSpec[] _genericTypeParameters;
        public GenericParameterSpec[] GenericTypeParameters => _genericTypeParameters ??= TryGetGenericTypeParameters();

        private GenericParameterSpec[] TryGetGenericTypeParameters()
        {
            var genericArgumentSpecs = _specManager
                .LoadTypeSpecs<GenericParameterSpec>(_methodDefinition.GenericParameters).ToArray();
            {
                foreach (var genericArgSpec in genericArgumentSpecs)
                {
                    genericArgSpec.RegisterAsGenericTypeParameterFor(this);
                }
            }
            return genericArgumentSpecs;
        }

        public bool HasExactGenericTypeParameters(GenericParameterSpec[] genericTypeParameterSpecs)
        {
            if (genericTypeParameterSpecs.Length == GenericTypeParameters.Length)
            {
                for (int i = 0; i < GenericTypeParameters.Length; i++)
                {
                    if (!GenericTypeParameters[i].IsValidGenericTypeMatchFor(genericTypeParameterSpecs[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        protected override void BuildSpec()
        {
            base.BuildSpec();
            _genericTypeParameters = TryGetGenericTypeParameters();
        }

        public override string ToString()
        {
            return $"{ExplicitName}";
        }

        private string CreateGenericMethodName(MethodDefinition methodDefinition)
        {
            return $"{CreateExplicitMemberName(methodDefinition)}<{AggregateGenericTypeParameterNames(methodDefinition)}>";
        }

        private string AggregateGenericTypeParameterNames(MethodDefinition methodDefinition)
        {
            return methodDefinition.GenericParameters.Select(gp => gp.Name)
                .Aggregate((a,b) => $"{a}, {b}");
        }

        public override bool MatchesSpec(MethodSpec methodSpec)
        {
            return (methodSpec is GenericMethodSpec genericMethodSpec)
                && base.MatchesSpec(methodSpec)
                && HasExactGenericTypeParameters(genericMethodSpec.GenericTypeParameters);
        }
    }
}
