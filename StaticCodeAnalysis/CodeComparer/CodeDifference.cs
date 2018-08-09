using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public class CodeDifference<T> : ICodeDifference
		where T : IStaticCodeElement
	{
		public CodeDifference(T sourceElement, T targetElement, string feature)
		{
			if (sourceElement.GetType() != targetElement.GetType())
				throw new CodeCompareException(string.Format("Element Type Mismatch: {0} vs {1}.", sourceElement, targetElement));
			SourceElement = sourceElement;
			TargetElement = targetElement;
			Feature = feature;
		}

		public string ElementName
		{
			get
			{
				return SourceElement.Name;
			}
		}

		public string Description()
		{
            return ElementName + TextDifference;
		}

		public string TextDifference
		{
			get
			{
				if (Feature == "Content")
					return ": Content Change";
				return "." + Feature + ": " + SourceValue + " - " + TargetValue;
			}
		}

        private string getPropertyValue(T element, string feature)
        {
			if (feature == "Content")
			{
				return "Content Change";
			}
            var property = element.GetType().GetProperties().SingleOrDefault(p => p.Name == feature);
            if (property == null)
                throw new CodeCompareException("Feature not found: " + feature);
            var propertyValue = property.GetValue(element);
            return propertyValue == null ? ""  : propertyValue.ToString();
        }

		public string Feature { get; set; }

        public T SourceElement { get; set; }
		
		public T TargetElement { get; set; }
        
        public string SourceValue
        {
            get
            {
                if (TargetList.Any() || SourceList.Any())
                    return SourceList.AsCSV();
                return getPropertyValue(SourceElement, Feature).ToString();
            }
        }

        public string TargetValue
        {
            get
            {
                if (TargetList.Any() || SourceList.Any())
                    return TargetList.AsCSV();
                return getPropertyValue(TargetElement, Feature);
            }
        }

        private List<string> _sourceList = new List<string>();
        public List<string> SourceList
        {
            get => _sourceList;
            set => _sourceList = value;
        }

        private List<string> _targetList = new List<string>();
        public List<string> TargetList
        {
            get => _targetList;
            set => _targetList = value;
        }

    }
}
