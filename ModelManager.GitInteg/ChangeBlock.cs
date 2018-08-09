using LibGit2Sharp;
using StaticCodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelManager.GitInteg
{
    public class ChangeBlock
    {
        public ChangeBlock(int lineStart, int linesAdded, int linesRemoved)
        {
            LineStart = lineStart;
            LinesAdded = linesAdded;
            LinesRemoved = linesRemoved;
        }

        public ChangeBlock(string lineStart, string linesAdded, string linesRemoved)
        {
            int.TryParse(lineStart, out _lineStart);
            int.TryParse(linesAdded, out _linesAdded);
            int.TryParse(linesRemoved, out _linesRemoved);
            ChangeBlockId = ChangeBlockCount++;
            ChangeBlocks[ChangeBlockId] = this;
        }

        public int ChangeBlockId { get; }

        public static int ChangeBlockCount { get; set; }

        private static IDictionary<int, ChangeBlock> _changeBlocks = new Dictionary<int, ChangeBlock>();
        public static IDictionary<int, ChangeBlock> ChangeBlocks
        {
            get => _changeBlocks;
            set => _changeBlocks = value;
        }

        private int _lineStart;
        public int LineStart
        {
            get => _lineStart;
            set => _lineStart = value;
        }

        private int _linesAdded;
        public int LinesAdded
        {
            get => _linesAdded;
            set => _linesAdded = value;
        }

        private int _linesRemoved;
        public int LinesRemoved
        {
            get => _linesRemoved;
            set => _linesRemoved = value;
        }

        public string Content { get; set; }

        public string MatchPattern
        {
            get
            {
                if (LineStart != 0)
                    return @"\@\@\s\-" + LineStart + "," + LinesRemoved + @"\s\+" + LineStart + "," + LinesAdded + @"\s\@\@\s[^\n]*\n";
                return @"\@\@\s\-" + LinesRemoved + @"\s\+" + LinesAdded + @"\s\@\@\s[^\n]*\n";
            }
        }

        
    }
}
