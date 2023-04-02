using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Extensions
{
    public static class CecilExtensions
    {
        const string FILENAME_REGEX = @"^(?'FileNameNoExtension'[\.\w]+)\.\w+$";

        public static string GetScopeNameWithoutExtension(this IMetadataScope scope)
        {
            var match = Regex.Match(scope.Name, FILENAME_REGEX);
            return match.Success ? match.Groups["FileNameNoExtension"].Value : scope.Name;
        }
    }
}
