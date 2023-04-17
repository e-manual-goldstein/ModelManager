using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class SystemModuleSpec : ModuleSpec
    {
        static string[] _systemModuleNames = new string[] { "mscorlib", "System.Core" };

        public SystemModuleSpec(ModuleDefinition module, string filePath, ISpecManager specManager)
            : base(module, filePath, specManager)
        {

        }

        public static bool IsSystemModule(IMetadataScope metadataScope)
        {
            var scopeName = metadataScope.GetScopeNameWithoutExtension();
            return _systemModuleNames.Contains(scopeName);
        }

        public override bool IsSystem => true;

        public static string GetSystemModuleName(IMetadataScope scope)
        {
            return "System.Core";
        }

        public override TypeSpec LoadTypeSpec(TypeReference type)
        {
            return base.LoadTypeSpec(type);
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override void BuildSpec()
        {
            base.BuildSpec();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        
    }
}
