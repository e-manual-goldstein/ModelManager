using AssemblyAnalyser.TestData.Basics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

namespace AssemblyAnalyser.TestData.WithExternalDependencies
{
    [NotTested]
    public class ClassWithMethodBodyDependency
    {        
        public string MethodWithDependencyInBody()
        {
            var newObject = new BasicClass();
            return JsonConvert.SerializeObject(newObject);
        }        
    }

    [NotTested]
    public class ClassWithMethodParameterDependency
    {
        public void MethodWithParameterDependency(JObject jObjectParam)
        {
            
        }
    }

    [NotTested]
    public class ClassWithMethodReturnTypeDependency
    {
        public JObject MethodWithReturnTypeDependency()
        {
            throw new NotImplementedException();
        }
    }

    [NotTested]
    public class ClassWithMethodGenericTypeParameterDependency
    {
        public void MethodWithGenericTypeParameterDependency<TGeneric>() where TGeneric : JObject
        {

        }
    }

    [NotTested]
    public class ClassWithInterfaceDependency : IJsonLineInfo
    {
        public int LineNumber { get; }
        public int LinePosition { get; }

        public bool HasLineInfo()
        {
            throw new System.NotImplementedException();
        }
    }

    [NotTested]
    public class ClassWithPropertyDependency
    {
        public JObject JObjectProperty { get; set; }
    }

    [NotTested]
    public class ClassWithFieldDependency
    {
        public JObject JObjectField;
    }

    [NotTested]
    public class ClassWithEventDependency
    {
        public event ExtensionDataGetter JObjectEvent;
    }
}
