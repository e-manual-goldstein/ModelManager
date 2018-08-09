using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeStructures
{
    public class CodeString : ICodeSymbol
    {
		private static IDictionary<int, CodeString> _codeStrings = new Dictionary<int, CodeString>();
		public static IDictionary<int, CodeString> CodeStrings
		{
			get => _codeStrings;
			set => _codeStrings = value;
		}

		private static int _complexStringCount = 0;
		public static int ComplexStringCount { get => _complexStringCount; set => _complexStringCount = value; }

		public CodeString(string stringContent, CodeFile codeFile)
        {
            CodeStringId = ComplexStringCount++;
            CodeStrings[CodeStringId] = this;
            StringContent = stringContent;
            File = codeFile;
        }

        public string StringContent { get; set; }

        public int CodeStringId { get; set; }

        public CodeFile File { get; set; }

        public override string ToString()
        {
            return StringContent;
        }

		public static string ExpandContent(string content, bool includeSymbols = false)
		{
			var stringPattern = @"STRING#(?'BlockId'[0-9]+)#";
			var match = Regex.Match(content, stringPattern);
			while (match.Success)
			{
				content = ExpandString(content, match.Groups["BlockId"].Value, includeSymbols);
				match = Regex.Match(content, stringPattern);
			}
			return content;
		}

		public static string ExpandString(string content, string stringId, bool includeSymbols = false)
		{
			int id = 0;
			if (int.TryParse(stringId, out id))
			{
				var replacementText = includeSymbols ? "\"" + CodeStrings[id].StringContent + "\"" : CodeStrings[id].StringContent;
				content = content.Replace("STRING#" + stringId + "#", CodeStrings[id].StringContent);
			}
			return content;
		}
    }
}
