using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    internal class GenericParameterSpec : TypeSpec
    {
        GenericParameter _genericParameter;

        public GenericParameterSpec(GenericParameter genericParameter, ISpecManager specManager) 
            : base($"{genericParameter.Namespace}.{genericParameter.Name}", genericParameter.FullName, specManager)
        {
            _genericParameter = genericParameter;
        }

        protected override ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByScopeName(_genericParameter.Module, _genericParameter.Scope);
        }
    }
}
