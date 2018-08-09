using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.Types
{
	public enum ModifierType
	{
		All = 0,
		Type = 1,
		Member = 2,
		TypeAndMember = 3,
		TypeAccess = 4,
		MemberAccess = 8,
		TypeAndMemberAccess = 12
	}
}
