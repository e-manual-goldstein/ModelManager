using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace StaticCodeAnalysis.Types
{
    public class CodeProjectFile
    {
        public CodeProjectFile(string filePath, bool analyse = false)
        {
            if (!File.Exists(filePath))
                throw new ArgumentException("File not found");
            Content = File.ReadAllText(filePath);
            FilePath = filePath;
            IncludedCodeFiles = new List<CodeFile>();
            ItemGroups = new Dictionary<string, List<string>>();
            DefinedItemGroups = new List<string>();
            GetItemGroups();
            GetAllIncludedElements();
            if (analyse)
                GetAllCodeFiles();
        }
        
        public string FileName { get; set; }

        public string AssemblyName { get; set; }

        public string FilePath { get; set; }

        public string Content { get; set; }

        public List<CodeFile> IncludedCodeFiles { get; set; }

        public List<string> DefinedItemGroups { get; set; }

        public Dictionary<string, List<string>> ItemGroups { get; set; }

        private void addToItemGroups(string key, string entry)
        {
            if (!ItemGroups.ContainsKey(key))
                ItemGroups[key] = new List<string>() { entry };
            else
                ItemGroups[key].Add(entry);
        }

        public List<string> DefinedPropertyGroups { get; set; }

        public Dictionary<string, List<string>> PropertyGroups { get; set; }

        private void addToPropertyGroups(string key, string entry)
        {
            if (!PropertyGroups.ContainsKey(key))
                PropertyGroups[key] = new List<string>() { entry };
            else
                PropertyGroups[key].Add(entry);
        }

        public void GetItemGroups()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException("File not found");
            var csprojFile = File.ReadAllText(FilePath).ToString();
            var fileMatches = Regex.Matches(csprojFile, @"\<ItemGroup\>(?'ItemGroup'.*?)\<\/ItemGroup\>", RegexOptions.Singleline) as ICollection;
            foreach (Match item in fileMatches)
            {
                DefinedItemGroups.Add(item.Groups["ItemGroup"].Value);
            }
        }

        public void GetAllIncludedElements()
        {
            foreach (var itemGroup in DefinedItemGroups)
            {
                var fileMatches = Regex.Matches(itemGroup, @"\<(?'BuildAction'[\w]*) Include=""(?'Element'.*?)""(\s*\/\>|.*?\k'BuildAction'\>)", RegexOptions.Singleline) as ICollection;


                foreach (Match item in fileMatches)
                {
                    var includeMode = item.Groups["BuildAction"].Value;
                    addToItemGroups(includeMode, item.Groups["Element"].Value);
                }
            }
        }

        public void GetAllCodeFiles()
        {
            var folderPath = Path.GetDirectoryName(FilePath);
            foreach (var element in ItemGroups["Compile"])
            {
                IncludedCodeFiles.Add(CodeUtils.ReadCodeFile(Path.Combine(folderPath, element)));
            }
        }

        public void GetAllPropertyGroups()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException("File not found");
            var csprojFile = File.ReadAllText(FilePath).ToString();
            var fileMatches = Regex.Matches(csprojFile, @"\<PropertyGroup\>(?'DefinedPropertyGroup'.*?)\<\/PropertyGroup\>", RegexOptions.Singleline) as ICollection;
            foreach (Match item in fileMatches)
            {
                DefinedPropertyGroups.Add(item.Groups["DefinedPropertyGroup"].Value);
            }
        }


    }
}