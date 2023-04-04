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

                }
            }
            return match.Success ? match.Groups["FileNameNoExtension"].Value : scope.Name;
        }        
    }
}
