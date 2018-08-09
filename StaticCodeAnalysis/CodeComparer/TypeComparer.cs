using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class TypeComparer : ElementComparer<TypeDefinition> 
		
	{
		public TypeComparer(TypeDefinition sourceType, TypeDefinition targetType, ICodeComparer parent) :
			base(sourceType, targetType, parent)
		{

		}

		public override void Compare()
		{
			base.Compare();

            compareProperties();
			compareFields();
			compareMethods();

		    CompareElements(SourceElementDefinition.NestedTypes, TargetElementDefinition.NestedTypes, "NestedTypes");
            CompareLists(SourceElementDefinition.Extensions, TargetElementDefinition.Extensions, "Extensions");
			if (SourceElementDefinition is EnumDefinition)
				compareEnums();
						
		}

		private void compareEnums()
		{
			//Is there anything other than basic content to compare?
		}

        private void compareProperties()
        {
            CompareElements(SourceElementDefinition.Properties, TargetElementDefinition.Properties, "Properties");
        }

		#region Class Elements

		private void compareMethods()
        {
            CompareElements(SourceElementDefinition.Methods, TargetElementDefinition.Methods, "Methods");
		}

		private void compareFields()
		{
			CompareElements(SourceElementDefinition.Fields, TargetElementDefinition.Fields, "Fields");
		}


		#endregion
	}
}
