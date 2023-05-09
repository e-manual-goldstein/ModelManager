﻿using AssemblyAnalyser.Extensions;
using AssemblyAnalyser.Specs;
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
        static string[] _systemModuleNames = new string[] { 
            "mscorlib", 
            "System.Core",
            "System.Configuration.ConfigurationManager",
            "System.Runtime", 
            "System.Data.Common", 
            "System.Private.CoreLib", 
            "System.Private.DataContractSerialization",
            "System.Private.Xml"
        };

        public SystemModuleSpec(ModuleDefinition module, string filePath, AssemblySpec assemblySpec, ISpecManager specManager)
            : base(module, filePath, assemblySpec, specManager)
        {
            ModuleShortName = SystemAssemblySpec.SYSTEM_MODULE_NAME;
        }

        public static bool IsSystemModule(IMetadataScope metadataScope)
        {
            var scopeName = metadataScope.GetScopeNameWithoutExtension();
            if (_systemModuleNames.Contains(scopeName))
            {
                return true;
            }
            return false;
        }

        public override bool IsSystem => true;

        public override TypeSpec LoadTypeSpec(TypeReference type)
        {
            return type switch
            {
                ArrayType arrayType => LoadFullTypeSpec(arrayType),
                TypeDefinition typeDefinition => LoadFullTypeSpec(typeDefinition),
                GenericInstanceType genericInstanceType => LoadFullTypeSpec(genericInstanceType),
                GenericParameter genericParameter=> LoadFullTypeSpec(genericParameter),
                _ => base.LoadTypeSpec(type.Resolve())
            };
            
        }

        protected override CustomAttribute[] GetAttributes()
        {
            return base.GetAttributes();
        }

        protected override void BuildSpec()
        {
            foreach (var exportedType in _baseVersion.ExportedTypes)
            {
                LoadTypeSpec(exportedType.Resolve());
            }
        }

        public override string ToString()
        {
            return SystemAssemblySpec.SYSTEM_MODULE_NAME;
        }

        
    }
}
