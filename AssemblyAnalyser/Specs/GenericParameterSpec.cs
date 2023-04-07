using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    public class GenericParameterSpec : TypeSpec
    {
        GenericParameter _genericParameter;

        public GenericParameterSpec(GenericParameter genericParameter, ISpecManager specManager) 
            : base($"{genericParameter.Namespace}.{genericParameter.Name}", genericParameter.FullName, specManager)
        {
            _genericParameter = genericParameter;
            Name = _genericParameter.Name;
            HasDefaultConstructorConstraint = _genericParameter.Attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
        }

        public bool HasDefaultConstructorConstraint { get; }

        protected override ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByScopeName(_genericParameter.Module, _genericParameter.Scope);
        }

        protected override TypeSpec CreateBaseSpec()
        {
            foreach (var constraint in _genericParameter.Constraints)
            {
                if (_specManager.TryLoadTypeSpec(() => constraint.ConstraintType, out TypeSpec typeSpec))
                {
                    if (typeSpec.IsClass)
                    {
                        return typeSpec;
                    }
                }
            }
            return null;
        }

        public override bool IsGenericParameter => true;
    }
}
