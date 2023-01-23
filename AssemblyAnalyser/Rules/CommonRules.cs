using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyAnalyser
{
    public static class CommonRules
    {
        public static InclusionRule<AssemblySpec> IncludeAssemblyByFullName(string assemblyName)
        {
            return new InclusionRule<AssemblySpec>(spec => spec.AssemblyFullName == assemblyName);
        }

        public static InclusionRule<AssemblySpec> IncludeAssemblyByShortName(string shortName)
        {
            return new InclusionRule<AssemblySpec>(spec => spec.AssemblyShortName == shortName);
        }

        public static ExclusionRule<AssemblySpec> ExcludeAssemblyByFullName(string assemblyName)
        {
            return new ExclusionRule<AssemblySpec>(spec => spec.AssemblyFullName == assemblyName);
        }

        public static ExclusionRule<AssemblySpec> ExcludeAssemblyByShortName(string assemblyName)
        {
            return new ExclusionRule<AssemblySpec>(spec => spec.AssemblyShortName == assemblyName);
        }

        public static InclusionRule<TypeSpec> IncludeTypesByAssembly()
        {
            return new InclusionRule<TypeSpec>(spec => spec.Assembly != null && spec.Assembly.Included());
        }

        public static ExclusionRule<TypeSpec> ExcludeTypesByAssembly()
        {
            return new ExclusionRule<TypeSpec>(spec => spec.Assembly.Excluded());
        }

        public static InclusionRule<ParameterSpec> IncludeParameterByType()
        {
            return new InclusionRule<ParameterSpec>(spec => spec.ParameterType.Included());
        }

        public static ExclusionRule<ParameterSpec> ExcludeParameterByType()
        {
            return new ExclusionRule<ParameterSpec>(spec => spec.ParameterType.Excluded());
        }

        public static InclusionRule<MethodSpec> IncludeMethodByType()
        {
            return new InclusionRule<MethodSpec>(spec => spec.ReturnType.Included());
        }

        public static ExclusionRule<MethodSpec> ExclusionMethodByType()
        {
            return new ExclusionRule<MethodSpec>(spec => spec.ReturnType.Excluded());
        }

        public static InclusionRule<PropertySpec> IncludePropertyByType()
        {
            return new InclusionRule<PropertySpec>(spec => spec.PropertyType.Included());
        }

        public static ExclusionRule<PropertySpec> ExcludePropertyByType()
        {
            return new ExclusionRule<PropertySpec>(spec => spec.PropertyType.Excluded());
        }
    }
}
