using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Extensions
{
    public static class CecilExtensions
    {
        const string FILENAME_REGEX = @"^(?'FileNameNoExtension'[\.\w]+)(\.\w+)$";
        public static ConcurrentDictionary<string, string> Removals = new ConcurrentDictionary<string, string>();
        public static string GetScopeNameWithoutExtension(this IMetadataScope scope)
        {
            switch (scope.MetadataScopeType)
            {
                case MetadataScopeType.AssemblyNameReference:
                    return scope.Name;
                case MetadataScopeType.ModuleReference:
                    break;
                case MetadataScopeType.ModuleDefinition:
                    break;
                default:
                    break;
            }
            var match = Regex.Match(scope.Name, FILENAME_REGEX);
            if (match.Success)
            {
                Removals.GetOrAdd($"{scope.MetadataScopeType}_{match.Groups[0].Value}", 
                    (str) => $"Removed {match.Groups[1]} from {match.Groups[0]} leaving {match.Groups[2]}\t{scope.MetadataScopeType}");
                if (match.Groups[1].Value != ".dll")
                {
                    Console.WriteLine($"Removed {match.Groups[1].Value} extension from Scope name");
                }
            }
            return match.Success ? match.Groups["FileNameNoExtension"].Value : scope.Name;
        }

        public static string CreateUniqueTypeSpecName(this TypeReference type, bool isArray)
        {
            var suffix = isArray ? "[]" : null;
            if (type is GenericParameter genericParameter)
            {
                if (genericParameter.DeclaringMethod == null)
                {
                    return $"{genericParameter.DeclaringType.FullName}[{type.FullName}]{suffix}";
                }
                else
                {
                    if (genericParameter.DeclaringType != null)
                    {

                    }
                    else
                    {
                        var declaringMethod = genericParameter.DeclaringMethod;
                        return $"{declaringMethod.DeclaringType.FullName}.{declaringMethod.Name}<{type.FullName}>{suffix}";
                    }
                }
            }
            if (type is GenericInstanceType genericInstanceType)
            {
                return CreateGenericArgumentsAggregateName(genericInstanceType);
            }
            return $"{type.FullName}";
        }

        public static string CreateGenericArgumentsAggregateName(this GenericInstanceType genericType)
        {
            var prefix = $"{genericType.Namespace}.{genericType.Name}<";
            var argumentNames = genericType.GenericArguments.Select(g => CreateUniqueTypeSpecName(g, false)).ToArray();
            var argumentString = argumentNames.Aggregate((a, b) => $"{a}, {b}");
            return $"{prefix}{argumentString}>";
        }



        public static string CreateUniqueMethodName(this MethodDefinition methodDefinition)
        {
            return methodDefinition.HasGenericParameters
                ? $"{methodDefinition.CreateGenericMethodName()}({methodDefinition.AggregateParameterNames()})"
                : $"{methodDefinition.CreateExplicitMemberName()}({methodDefinition.AggregateParameterNames()})";
        }

        public static string CreateGenericMethodName(this MethodDefinition methodDefinition)
        {
            return $"{CreateExplicitMemberName(methodDefinition)}<{AggregateGenericTypeParameterNames(methodDefinition)}>";
        }

        private static string AggregateParameterNames(this MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasParameters)
            {
                return string.Empty;
            }
            return methodDefinition.Parameters.Select(p => $"{p.ParameterType} {p.Name}")
                .Aggregate((a, b) => $"{a}, {b}");
        }

        private static string AggregateGenericTypeParameterNames(this MethodDefinition methodDefinition)
        {
            return methodDefinition.GenericParameters.Select(gp => gp.Name)
                .Aggregate((a, b) => $"{a}, {b}");
        }

        public static string CreateExplicitMemberName(this MethodDefinition methodDefinition)
        {
            return $"{methodDefinition.DeclaringType.FullName}.{methodDefinition.Name}";
        }
    }
}
