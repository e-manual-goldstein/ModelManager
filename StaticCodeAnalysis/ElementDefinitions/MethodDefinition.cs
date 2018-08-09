using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using StaticCodeAnalysis.CodeStructures;

namespace StaticCodeAnalysis.Types
{
    public class MethodDefinition : MemberDefinition, IHasParameters
    {
        public MethodDefinition(string methodDeclaration, string methodContent, IStaticCodeElement owner) : base(methodDeclaration, methodContent, owner)
        {
            Parameters = new List<DeclaredParameter>();
            getMethodParameters(methodDeclaration);
			checkForOverloads();
        }

		public List<DeclaredParameter> Parameters { get; set; }

		public DefinedMethodType MethodType { get; set; }

		public string BaseMethodDefinition { get; set; }

        //TODO: This should be plural
		public string GenericMethodType { get; set; }

		private string _condensedGenericMethodTypeConditions;
		public string GenericMethodTypeConditions
		{
			get
			{
				return !string.IsNullOrWhiteSpace(_condensedGenericMethodTypeConditions) ?
					CodeUtils.ExpandAllSymbols(_condensedGenericMethodTypeConditions) : "";
			}
		}

		public override string Name
		{
			get
			{
				return SimpleName + OverloadVersion;
			}
			set
			{
				SimpleName = value;
			}
		}

		public string SimpleName { get; set; }

		public int OverloadVersion { get; set; }

		private void getMethodParameters(string methodDeclaration)
        {
			if (!string.IsNullOrWhiteSpace(BaseMethodDefinition))
				methodDeclaration = methodDeclaration.Replace(BaseMethodDefinition, "");
			if (!string.IsNullOrWhiteSpace(_condensedGenericMethodTypeConditions))
				methodDeclaration = methodDeclaration.Replace(_condensedGenericMethodTypeConditions, "");
			var parameterBlock = ParenthesisBlock.InnerBlock(methodDeclaration);
			var inlineParameterBlock = parameterBlock.AsSingleLine();
			if (string.IsNullOrEmpty(inlineParameterBlock))
				return;
			var condensedParamBlock = GenericTypeBlock.SymboliseBlock(inlineParameterBlock, this);
			condensedParamBlock = AttributeBlock.SymboliseBlock(condensedParamBlock,this);
			foreach (var parameter in condensedParamBlock.Split(','))
			{
				Parameters.Add(new DeclaredParameter(parameter.Trim(), this));
			}
		}

        protected override string getMemberNameFromDeclaration(string methodDeclaration)
        {
			var inlineMethodDeclaration = methodDeclaration.AsSingleLine();
			var constructorPattern = @"^(?'MemberName'\w+)\s*__PARENTHESIS__[0-9]+__(?'SuperConstructor'$|\s*\:\s*(this|base)\s*__PARENTHESIS__[0-9]+__$)";
			var constructorName = Regex.Match(inlineMethodDeclaration, constructorPattern).Groups["MemberName"].Value;
			if (!string.IsNullOrWhiteSpace(constructorName))
			{
				MethodType = DefinedMethodType.Constructor;
				if (!string.IsNullOrWhiteSpace(Regex.Match(inlineMethodDeclaration, constructorPattern).Groups["SuperConstructor"].Value))
				{
					var baseMethodDefinition = CodeUtils.ExpandAllSymbols(Regex.Match(inlineMethodDeclaration, constructorPattern).Groups["SuperConstructor"].Value.Trim());
					BaseMethodDefinition = baseMethodDefinition;
				}
				ReturnType = ".ctor";
				return constructorName;
			}
			var basicMethodPattern = @"^(?'ReturnType'[\.\[\]\w\s\<\>\,\?]*)\s(?'MemberName'[\.\w]+)\s*__PARENTHESIS__[0-9]+__$";
			var basicMethodName = Regex.Match(inlineMethodDeclaration, basicMethodPattern).Groups["MemberName"].Value;
			if (!string.IsNullOrWhiteSpace(basicMethodName))
			{
				ReturnType = CodeUtils.ExpandAllSymbols(Regex.Match(inlineMethodDeclaration, basicMethodPattern).Groups["ReturnType"].Value.Trim(),true);
				if (string.IsNullOrWhiteSpace(ReturnType))
					throw new CodeParseException("Could not parse Return Type for Member");
				if (Regex.Match(basicMethodName, "__OPERATOR__[0-9]+__").Success)
				{
					MethodType = DefinedMethodType.Operator;
					ReturnType = ReturnType.Replace("operator", "").Trim();
					return Operator.ExpandContent(basicMethodName);
				}
				MethodType = DefinedMethodType.Basic;
				return basicMethodName;
			}
			var genericTypePattern = @"^(?'ReturnType'[\w\s\<\>\,]+)\s(?'MemberName'\w+)<(?'GenericType'[\w\.\s\,]+)>\s*__PARENTHESIS__[0-9]+__($|(?'GenericTypeConditions'[\w\s]+\:[\:\<\>\w\s\,\(\)]+))";
			var genericMethodMatch = Regex.Match(inlineMethodDeclaration, genericTypePattern);
			if (!string.IsNullOrWhiteSpace(genericMethodMatch.Groups["MemberName"].Value))
			{
				MethodType = DefinedMethodType.Generic;
				GenericMethodType = genericMethodMatch.Groups["GenericType"].Value;
				ReturnType = genericMethodMatch.Groups["ReturnType"].Value;
				if (!string.IsNullOrWhiteSpace(genericMethodMatch.Groups["GenericTypeConditions"].Value))
				{
					_condensedGenericMethodTypeConditions = genericMethodMatch.Groups["GenericTypeConditions"].Value.Trim();
				}
				return genericMethodMatch.Groups["MemberName"].Value;
			}
			throw new CodeParseException("Could not parse Method Name");
        }

		private void checkForOverloads()
		{
			var overloadMethods = Owner.Elements.OfType<MethodDefinition>().Where(c => c.SimpleName == SimpleName);
			if (overloadMethods.Any())
			{
				OverloadVersion = overloadMethods.Count();
			}
		}

		public override string ToString()
		{
			return "Method Definition: " + Name;
		}

	}
}
