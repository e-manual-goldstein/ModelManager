using AssemblyAnalyser.TestData.Basics;
using AssemblyAnalyser.TestData.Complex;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

namespace AssemblyAnalyser.TestData.WithDependencies
{
    public class ClassWithMethodBodyDependency
    {        
        public string MethodWithDependencyInBody()
        {
            var newObject = new BasicClass();
            return JsonConvert.SerializeObject(newObject);
        }        
    }

    public class ClassWithMethodParameterDependency
    {
        public void MethodWithParameterDependency(JObject jObjectParam)
        {
            
        }
    }

    public class ClassWithMethodReturnTypeDependency
    {
        public JObject MethodWithReturnTypeDependency()
        {
            throw new NotImplementedException();
        }
    }

    public class ClassWithMethodGenericTypeParameterDependency
    {
        public void MethodWithGenericTypeParameterDependency<TGeneric>() where TGeneric : JObject
        {

        }
    }

    public class ClassWithInterfaceDependency : IJsonLineInfo
    {
        public int LineNumber { get; }
        public int LinePosition { get; }

        public bool HasLineInfo()
        {
            throw new System.NotImplementedException();
        }
    }

    public class ClassWithPropertyDependency
    {
        public JObject JObjectProperty { get; set; }
    }

    public class ClassWithFieldDependency
    {
        public JObject JObjectField;
    }

    public class ClassWithEventDependency
    {
        public event ExtensionDataGetter JObjectEvent;
    }
}
