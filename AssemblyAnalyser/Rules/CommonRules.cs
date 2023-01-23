using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyAnalyser
{
    public static class CommonRules
    {
        public static ExclusionRule ExcludeByAssemblyName(string assemblyName)
        {
            return new ExclusionRule(spec =>
            {
                return spec switch
                {
                    AssemblySpec assemblySpec => assemblySpec.MatchesName(assemblyName),
                    TypeSpec typeSpec => typeSpec.Assembly?.MatchesName(assemblyName) ?? false,   
                    MethodSpec methodSpec => methodSpec.DeclaringType?.Assembly?.MatchesName(assemblyName) ?? false,
                    ParameterSpec parameterSpec => parameterSpec.Method?.DeclaringType.Assembly?.MatchesName(assemblyName) ?? false,
                    PropertySpec propertySpec => propertySpec.PropertyType?.Assembly?.MatchesName(assemblyName) ?? false,
                    FieldSpec fieldSpec => fieldSpec.FieldType?.Assembly?.MatchesName(assemblyName) ?? false,
                    _ => false
                };
            });
        }

        public static InclusionRule IncludeByAssemblyName(string assemblyName)
        {
            return new InclusionRule(spec =>
            {
                return spec switch
                {
                    AssemblySpec assemblySpec => assemblySpec.MatchesName(assemblyName),
                    TypeSpec typeSpec => typeSpec.Assembly?.MatchesName(assemblyName) ?? false,
                    MethodSpec methodSpec => methodSpec.DeclaringType?.Assembly?.MatchesName(assemblyName) ?? false,
                    ParameterSpec parameterSpec => parameterSpec.Method?.DeclaringType.Assembly?.MatchesName(assemblyName) ?? false,
                    PropertySpec propertySpec => propertySpec.PropertyType?.Assembly?.MatchesName(assemblyName) ?? false,
                    FieldSpec fieldSpec => fieldSpec.FieldType?.Assembly?.MatchesName(assemblyName) ?? false,
                    _ => false
                };
            });
        }

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
            return new InclusionRule<TypeSpec>(spec => spec.Assembly != null && spec.Assembly.IsIncluded());
        }

        public static ExclusionRule<TypeSpec> ExcludeTypesByAssembly()
        {
            return new ExclusionRule<TypeSpec>(spec => spec.Assembly.IsExcluded());
        }

        public static InclusionRule<ParameterSpec> IncludeParameterByType()
        {
            return new InclusionRule<ParameterSpec>(spec => spec.ParameterType.IsIncluded());
        }

        public static ExclusionRule<ParameterSpec> ExcludeParameterByType()
        {
            return new ExclusionRule<ParameterSpec>(spec => spec.ParameterType.IsExcluded());
        }

        public static InclusionRule<MethodSpec> IncludeMethodByType()
        {
            return new InclusionRule<MethodSpec>(spec => spec.ReturnType.IsIncluded());
        }

        public static ExclusionRule<MethodSpec> ExclusionMethodByType()
        {
            return new ExclusionRule<MethodSpec>(spec => spec.ReturnType.IsExcluded());
        }

        public static InclusionRule<PropertySpec> IncludePropertyByType()
        {
            return new InclusionRule<PropertySpec>(spec => spec.PropertyType.IsIncluded());
        }

        public static ExclusionRule<PropertySpec> ExcludePropertyByType()
        {
            return new ExclusionRule<PropertySpec>(spec => spec.PropertyType.IsExcluded());
        }
    }
}
