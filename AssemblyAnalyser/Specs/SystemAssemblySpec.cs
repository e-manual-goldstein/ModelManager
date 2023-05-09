using AssemblyAnalyser.Extensions;
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
        public const string SYSTEM_ASSEMBLY_NAME = "CoreSystemAssembly";
        public const string SYSTEM_MODULE_NAME = "CoreSystemModule";
        Dictionary<string, IMetadataScope> _metadataScopes = new();

        public SystemAssemblySpec(AssemblyDefinition assemblyDefinition, string filePath, 
            IAssemblyLocator assemblyLocator, ISpecManager specManager) 
            : base(assemblyDefinition, filePath, assemblyLocator, specManager)
        {
            AssemblyShortName = SYSTEM_ASSEMBLY_NAME;
        }

        
        public override bool IsSystem => true;

        protected override ModuleSpec CreateFullModuleSpec(IMetadataScope scope)
        {
            var moduleDefinition = scope as ModuleDefinition
                ?? _assemblyDefinition.Modules.SingleOrDefault();
            if (moduleDefinition != null)
            {
                return new SystemModuleSpec(moduleDefinition, moduleDefinition.FileName, this, _specManager);
            }
            _specManager.AddFault(this, FaultSeverity.Critical, "System Assembly has no Modules");
            return CreateMissingModuleSpec(scope as AssemblyNameReference);
        }

        public override ModuleSpec LoadModuleSpecForTypeReference(TypeReference typeReference)
        {
            return _moduleSpecs.GetOrAdd(SYSTEM_MODULE_NAME, (key) => CreateFullModuleSpec(typeReference.Scope));
        }

        public override AssemblySpec RegisterMetaDataScope(IMetadataScope scope)
        {
            _metadataScopes.TryAdd(scope.GetUniqueNameFromScope(), scope);
            return this;
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

        public AssemblySpec WithReferencedAssembly(AssemblyNameReference assemblyReference)
        {
            RegisterMetaDataScope(assemblyReference);
            return this;
        }
    }
}
