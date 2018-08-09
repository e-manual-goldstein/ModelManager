using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelManager.Replicator
{
    public static class Replicate
    {
        private static Lazy<IDictionary<string, string>> _modifierLookup = new Lazy<IDictionary<string, string>>
        (() => {
			var dictionary = new Dictionary<string, string>();
            dictionary.Add("IsPublic", "public");
			dictionary.Add("IsAbstract", "abstract");
			dictionary.Add("IsFamilyAndAssembly", "protected internal");
			dictionary.Add("IsFamily", "protected");
			dictionary.Add("IsAssembly", "internal");
            dictionary.Add("IsStatic", "static");
            dictionary.Add("IsVirtual", "virtual");
            //TODO: Add more of these;
            return dictionary;
		});

        public static IDictionary<string, string> ModifierLookup
        {
            get => _modifierLookup.Value;
        }

        private static Lazy<IDictionary<string, int>> _modifierOrder = new Lazy<IDictionary<string, int>>
        (() => {
            var dictionary = new Dictionary<string, int>();
            dictionary.Add("public", 1);
            dictionary.Add("protected internal", 2);
            dictionary.Add("protected", 3);
            dictionary.Add("internal", 4);
            dictionary.Add("abstract", 5);
            dictionary.Add("static", 6);
            dictionary.Add("virtual", 7);
            //TODO: Add more of these as they arise;
            return dictionary;
        });

        public static IDictionary<string, int> ModifierOrder
        {
            get => _modifierOrder.Value;
        }
        
        private static Lazy<IDictionary<string, int>> _accessModifierOrder = new Lazy<IDictionary<string, int>>
        (() => {
            var dictionary = new Dictionary<string, int>();
            dictionary.Add("public", 1);
            dictionary.Add("protected internal",2);
            dictionary.Add("protected", 3);
            dictionary.Add("internal", 4);
            dictionary.Add("private", 5);
            return dictionary;
        });

        public static IDictionary<string, int> AccessModifierOrder
        {
            get => _accessModifierOrder.Value;
        }

        private static Lazy<IDictionary<string, string>> _keywordLookup = new Lazy<IDictionary<string, string>>
        (() => {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("String", "string");
            dictionary.Add("String[]", "string[]");
            dictionary.Add("Int32", "int");
            dictionary.Add("Int32[]", "int[]");
            dictionary.Add("Boolean", "bool");
            dictionary.Add("Void", "void");
            return dictionary;
        });

        public static IDictionary<string, string> KeywordLookup
        {
            get => _keywordLookup.Value;
        }

        private static StringBuilder MemberStub(string name, string returnType, string[] modifiers)
        {
            var memberBlock = new StringBuilder();
            foreach (var modifier in modifiers)
            {
                memberBlock.Append(modifier + " ");
            }
            memberBlock.Append(returnType + " ");
            memberBlock.Append(name);
            return memberBlock;
        }

        #region Method Replicators

        public static string Method(string name, string returnType, string[] modifiers, string[] parameters)
        {
            var methodBlock = MemberStub(name, returnType, modifiers);
            methodBlock.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    methodBlock.Append(", ");
                methodBlock.Append(parameters[i]);
            }
            methodBlock.AppendLine(")");
            methodBlock.AppendLine("{");
            methodBlock.AppendLine(@"   throw new NotImplementedException(""This is mock representation of the original implementation"");");
            methodBlock.AppendLine("}");
            methodBlock.AppendLine();
            return methodBlock.ToString();
        }

        public static string Method(MethodInfo baseMethod, bool fromInterface = false)
        {
            var name = baseMethod.Name;
            var returnType = normaliseTypeName(baseMethod.ReturnType);
            var modifiers = getModifiers(baseMethod, fromInterface);
            var parameters = createParameterArray(baseMethod.GetParameters());
            return Method(name, returnType, modifiers, parameters);
        }

        private static string[] createParameterArray(ParameterInfo[] parameterInfo)
        {
            var paramlist = new List<string>();
            foreach (var parameter in parameterInfo)
            {
                var parameterType = normaliseTypeName(parameter.ParameterType);
                var parameterName = parameter.Name;
                paramlist.Add(parameterType + " " + parameterName);
            }
            return paramlist.ToArray();
        }

        #endregion

        #region Property Replicators

        public static string Property(string name, string returnType, string[] getterModifiers, string[] setterModifiers, bool hasGetter, bool hasSetter)
        {
            var modifiers = aggregateModifiers(getterModifiers, setterModifiers);
            var propertyBlock = MemberStub(name, returnType, modifiers);
            propertyBlock.Append(" { ");
            if (hasGetter)
                propertyBlock.Append(@"get; ");
            if (hasSetter)
                propertyBlock.Append(@"set; ");
            propertyBlock.AppendLine("}");
            propertyBlock.AppendLine();
            return propertyBlock.ToString();
        }

        public static string Property(PropertyInfo baseProperty, bool fromInterface = false)
        {
            var propertyName = baseProperty.Name;
            var returnType = normaliseTypeName(baseProperty.PropertyType);
            var getter = baseProperty.GetGetMethod();
            var setter = baseProperty.GetSetMethod();
            var getterModifiers = getter != null ? getModifiers(getter, fromInterface) : new string[0];
            var setterModifiers = setter != null ? getModifiers(setter, fromInterface) : new string[0];
            var modifiers = getterModifiers.Intersect(setterModifiers).ToArray();
            var hasGetter = getter != null;
            var hasSetter = setter != null;
            return Property(propertyName, returnType, getterModifiers, setterModifiers, hasGetter, hasSetter);
        }

        private static string[] aggregateModifiers(string[] getterModifiers, string[] setterModifiers)
        {
            var allModifiers = getterModifiers.Union(setterModifiers).Distinct().ToList();
            var accessModifier = allModifiers.Where(m => AccessModifierOrder.ContainsKey(m)).OrderBy(m => AccessModifierOrder[m]).FirstOrDefault();
            allModifiers.Remove("public");
            allModifiers.Remove("protected internal");
            allModifiers.Remove("private protected");
            allModifiers.Remove("private");
            allModifiers.Remove("protected");
            allModifiers.Remove("internal");
            allModifiers.Add(accessModifier);
            return allModifiers.OrderBy(m => ModifierOrder[m]).ToArray();
        }

        #endregion

        #region Class Replicators

        public static string Class(string name, string[] modifiers, MemberInfo[] members, Type fromInterface = null)
        {
            var classBlock = new StringBuilder();
            foreach (var modifier in modifiers)
            {
                classBlock.Append(modifier + " ");
            }
            classBlock.Append("class ");
            classBlock.Append(name);
            var inheritance = fromInterface != null ? " : " + fromInterface.Name : "";
            classBlock.AppendLine(inheritance);
            classBlock.AppendLine("{");
            classBlock.Append(insertMembers(members, fromInterface != null));
            classBlock.AppendLine("}");
            return classBlock.ToString();
        }

        public static string Class(Type type)
        {
            var name = type.Name;
            var modifiers = type.GetModifiers();
            var members = type.GetMembers().Where(m => m.DeclaringType == type).ToArray();
            return Class(name, modifiers, members);
        }
        
        private static string insertMembers(MemberInfo[] members, bool fromInterface)
        {
            var memberBlock = new StringBuilder();
            var methods = members.OfType<MethodInfo>().Where(m => !m.IsSpecialName);
            foreach (var method in methods)
            {
                memberBlock.Append(Method(method, fromInterface));
            }
            var properties = members.OfType<PropertyInfo>();
            foreach (var property in properties)
            {
                memberBlock.Append(Property(property, fromInterface));
            }
            return memberBlock.ToString();
        }

        #endregion

        #region Private Helpers

        private static string normaliseTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var fullTypeName = new StringBuilder();
                var simpleTypeMatch = Regex.Match(type.Name, @"(?'TypeName'.*)\`[0-9]*$");
                fullTypeName.Append(simpleTypeMatch.Groups["TypeName"].Value + "<");
                var genArgs = type.GetGenericArguments();
                for (int i = 0; i < genArgs.Length; i++)
                {
                    if (i > 0)
                        fullTypeName.Append(", ");
                    var argTypeName = normaliseTypeName(genArgs[i]);
                    fullTypeName.Append(argTypeName);
                }
                fullTypeName.Append(">");
                return fullTypeName.ToString();
            }
            return insertKeywordsForSystemType(type.Name);
        }

        private static string insertKeywordsForSystemType(string name)
        {
            if (KeywordLookup.ContainsKey(name))
                return KeywordLookup[name];
            return name;
        }

        private static string[] getModifiers(MethodInfo baseMethod, bool fromInterface)
        {
            var modifiers = new List<string>();
            foreach (var modifier in ModifierLookup)
            {
                var property = baseMethod.GetType().BaseType.GetRuntimeProperties().FirstOrDefault(p => p.Name == modifier.Key);
                if (property.GetValue(baseMethod) as bool? ?? false)
                    modifiers.Add(modifier.Value);
            }
            if (fromInterface)
            {
                removeSpecialModifiers(modifiers);
                modifiers.Add("public");
            }
            return modifiers.ToArray();
        }

        private static void removeSpecialModifiers(List<string> modifiers)
        {
            modifiers.Remove("public");
            modifiers.Remove("protected");
            modifiers.Remove("internal");
            modifiers.Remove("abstract");
        }

        #endregion
    }
}
  