using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    public class SystemAssemblySpec : AssemblySpec
    {
        public SystemAssemblySpec(AssemblyDefinition assemblyDefinition, string filePath, ISpecManager specManager) 
            : base(assemblyDefinition, filePath, specManager)
        {
        }

        public override bool IsSystem => true;

        protected override ModuleSpec CreateFullModuleSpec(IMetadataScope scope)
        {
            if (scope is ModuleDefinition moduleDefinition)
            {
                return new SystemModuleSpec(moduleDefinition, moduleDefinition.FileName, _specManager);
            }                
            return base.CreateFullModuleSpec(scope);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void BuildSpec()
        {
            base.BuildSpec();
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }
    }
}
