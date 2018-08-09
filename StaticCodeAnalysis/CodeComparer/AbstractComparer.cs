using StaticCodeAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticCodeAnalysis.CodeComparer
{
	public abstract class AbstractComparer<T> : ICodeComparer
		where T : IStaticCodeElement
	{
		public AbstractComparer(T sourceElement, T targetElement, ICodeComparer parent)
		{
			if (sourceElement.GetType() != targetElement.GetType())
				throw new CodeCompareException(string.Format("Cannot compare {0} with {1}.", sourceElement, targetElement));
			SourceElementDefinition = sourceElement;
			TargetElementDefinition = targetElement;
			Differences = new List<ICodeDifference>();
			DescendantComparers = new List<ICodeComparer>();
            Parent = parent;
			Compare();
		}

		public T SourceElementDefinition { get; private set; }
		public T TargetElementDefinition { get; private set; }

		public abstract void Compare();

		public string DescribeDifferences()
		{
			var sb = new StringBuilder();
			sb.AppendLine(SourceElementDefinition.Name);
			foreach (var difference in Differences)
			{
				sb.AppendLine(difference.Description());
			}
			return sb.ToString();
		}

		public List<ICodeDifference> GetDifferences(bool recurse = false)
		{
			var differences = Differences;
			if (recurse)
			{
				foreach (var descendant in DescendantComparers)
				{
					differences.AddRange(descendant.GetDifferences(recurse));
				}
			}
			return differences;
		}

		public List<ICodeDifference> Differences { get; set; }

        public ICodeComparer Parent { get; set; }

		public List<ICodeComparer> DescendantComparers { get; private set; }

		public ICodeDifference CreateNewDifference(string feature)
		{
            var difference = new CodeDifference<T>(SourceElementDefinition, TargetElementDefinition, feature);
            AddDifference(difference);
            return difference;
        }

        public void AddDifference(ICodeDifference difference)
        {
            Differences.Add(difference);
            if (Parent != null)
                Parent.AddDifference(difference);
        }

		protected void CompareLists(List<string> sourceList, List<string> targetList, string itemDescriptor) 
		{
            var itemList = new List<string>();
            var sourceItems = sourceList.Except(targetList);
            if (sourceItems.Any())
			{
                var newDifference = CreateNewDifference(itemDescriptor);
                newDifference.SourceList.AddRange(sourceItems);
			}
            var targetItems = targetList.Except(sourceList);
            if (targetItems.Any())
            {
                var newDifference = CreateNewDifference(itemDescriptor);
                newDifference.TargetList.AddRange(targetItems);
            }
        }

        public void CompareElements<TElement>(List<TElement> sourceList, List<TElement> targetList, string itemDescriptor)
            where TElement : IStaticCodeElement
        {
            var sourceElements = sourceList.Select(ns => ns.Name).ToList();
            var targetElements = targetList.Select(ns => ns.Name).ToList();
            CompareLists(sourceElements, targetElements, itemDescriptor);
            var commonElements = sourceElements.Intersect(targetElements);
            foreach (var definedElement in commonElements)
            {
                var sourceElement = sourceList.SingleOrDefault(ns => ns.Name == definedElement);
                var targetElement = targetList.SingleOrDefault(ns => ns.Name == definedElement);
				var comparerType = lookupComparerType(typeof(TElement));
                var elementComparer = Activator.CreateInstance(comparerType, sourceElement, targetElement, this) as ICodeComparer;
                DescendantComparers.Add(elementComparer);
            }
        }

		private Type lookupComparerType(Type elementType)
		{
			return _elementTypeLookup.Value[elementType];
		}

		private static readonly Lazy<IDictionary<Type, Type>> _elementTypeLookup = new Lazy<IDictionary<Type, Type>>
		(() => {
			var dictionary = new Dictionary<Type, Type>();
			dictionary.Add(typeof(CodeFile), typeof(CodeFileComparer));
			dictionary.Add(typeof(NamespaceDefinition), typeof(NamespaceComparer));
			dictionary.Add(typeof(TypeDefinition), typeof(TypeComparer));
			dictionary.Add(typeof(FieldDefinition), typeof(FieldComparer));
			dictionary.Add(typeof(MethodDefinition), typeof(MethodComparer));
			dictionary.Add(typeof(PropertyDefinition), typeof(PropertyComparer));
			dictionary.Add(typeof(DeclaredParameter), typeof(ParameterComparer));
			dictionary.Add(typeof(DeclaredAttribute), typeof(AttributeComparer));
			return dictionary;
		});
	}
}
