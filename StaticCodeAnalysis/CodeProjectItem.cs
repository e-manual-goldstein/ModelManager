using StaticCodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis
{
    public class CodeProjectItem : ICodeProjectElement
    {
        public CodeProjectItem(string include, string buildAction, string content)
        {
            Include = include;
            BuildAction = buildAction;
            Content = content;
        }

        public string Include { get; set; }

        public string BuildAction { get; set; }

        public string Content { get; set; }
    }
}
