using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Replicator
{
    public static class ReplicatorExtensions
    {
        public static string[] GetModifiers(this Type baseType)
        {
            var modifiers = new List<string>();
            if (IsPublic(baseType))
                modifiers.Add("public");
            if (IsInternal(baseType))
                modifiers.Add("internal");
            if (IsProtected(baseType))
                modifiers.Add("protected");
            if (IsPrivate(baseType))
                modifiers.Add("private");
            if (baseType.IsAbstract && !baseType.IsInterface)
                modifiers.Add("abstract");
            //if (baseType.IsSealed)
            //    modifiers.Add("sealed");
            //TODO: Add more as the need arises
            return modifiers.ToArray();
        }

        public static bool IsPublic(this Type t)
        {
            return t.IsVisible && t.IsPublic && !t.IsNotPublic && !t.IsNested && !t.IsNestedPublic
                && !t.IsNestedFamily && !t.IsNestedPrivate && !t.IsNestedAssembly && !t.IsNestedFamORAssem && !t.IsNestedFamANDAssem;
        }

        public static bool IsInternal(this Type t)
        {
            return !t.IsVisible && !t.IsPublic && t.IsNotPublic && !t.IsNested && !t.IsNestedPublic
                && !t.IsNestedFamily && !t.IsNestedPrivate && !t.IsNestedAssembly && !t.IsNestedFamORAssem && !t.IsNestedFamANDAssem;
        }

        // only nested types can be declared "protected"
        public static bool IsProtected(this Type t)
        {
            return !t.IsVisible && !t.IsPublic && !t.IsNotPublic && t.IsNested && !t.IsNestedPublic
                && t.IsNestedFamily && !t.IsNestedPrivate && !t.IsNestedAssembly && !t.IsNestedFamORAssem && !t.IsNestedFamANDAssem;
        }

        // only nested types can be declared "private"
        public static bool IsPrivate(this Type t)
        {
            return !t.IsVisible && !t.IsPublic && !t.IsNotPublic && t.IsNested && !t.IsNestedPublic
                && !t.IsNestedFamily && t.IsNestedPrivate && !t.IsNestedAssembly && !t.IsNestedFamORAssem && !t.IsNestedFamANDAssem;
        }


    }
}
