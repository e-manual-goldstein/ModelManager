using AssemblyAnalyser.Specs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyAnalyser.Tests
{
    [TestClass]
    public class GenericTypeTests : AbstractSpecTests
    {
        const string NAMESPACE = "AssemblyAnalyser.TestData.Generics";

        const string CLASS_WITH_GENERIC_INTERFACE = "ClassImplementingGenericInterface";
        const string GENERIC_CLASS = "GenericClass`1";
        const string GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS = "GenericClassWithClassTypeConstraints`1";
        const string GENERIC_CLASS_WITH_DEFAULT_CONSTRUCTOR_TYPE_CONSTRAINTS = "GenericClassWithDefaultConstructorTypeConstraints`1";
        const string CLASS_WITH_GENERIC_METHODS = "ClassWithGenericMethods";
        const string CLASS_WITH_SYNONYMOUS_GENERIC_METHODS = "ClassWithSynonymousMethods";

        const string GENERIC_INTERFACE = "IGenericInterface`1";
        const string GENERIC_INTERFACE_WITH_TYPE_CONSTRAINTS = "IGenericInterfaceWithTypeConstraints`1";
        const string INTERFACE_WTIH_GENERIC_METHODS = "IInterfaceWithGenericMethods";

        private string[] GENERIC_TYPE_NAMES = new[] { GENERIC_CLASS, GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS, 
            GENERIC_CLASS_WITH_DEFAULT_CONSTRUCTOR_TYPE_CONSTRAINTS, CLASS_WITH_GENERIC_METHODS,
            GENERIC_INTERFACE, GENERIC_INTERFACE_WITH_TYPE_CONSTRAINTS, INTERFACE_WTIH_GENERIC_METHODS 
        };

        [TestInitialize]
        public override void Initialize()
        {
            _exceptionManager = new ExceptionManager();
            _loggerProvider = NSubstitute.Substitute.For<ILoggerProvider>();
            _specManager = new SpecManager(_loggerProvider, _exceptionManager);
            var filePath = "..\\..\\..\\..\\AssemblyAnalyser.TestData\\bin\\Debug\\net6.0\\AssemblyAnalyser.TestData.dll";
            _moduleSpec = _specManager.LoadAssemblySpecFromPath(Path.GetFullPath(filePath)).LoadModuleSpecFromPath(Path.GetFullPath(filePath));
            _moduleSpec.Process();
            
            _basicClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicClass");
        }

        #region Generic Type Tests

        [TestMethod]
        public void GenericTypeSpecsAreNotNull_Test()
        {
            foreach (var className in GENERIC_TYPE_NAMES)
            {
                var classSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{className}");
                Assert.IsNotNull(classSpec);
                Assert.IsFalse(classSpec.IsMissingSpec);
                Assert.IsFalse(classSpec.IsNullSpec);
            }
        }

        [TestMethod]
        public void GenericClassSpecHasGenericTypeParameter_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.IsNotNull(genericTypeParameterSpec);
            Assert.IsTrue(genericTypeParameterSpec.IsGenericParameter);
        }

        [TestMethod]
        public void GenericClassPropertySpecMatchesGenericTypeParameter_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault(t => t.Name == "TGenericForClass");
            var genericPropertySpec = genericTypeSpec.GetPropertySpec("GenericProperty");

            Assert.IsNotNull(genericPropertySpec);
            Assert.AreEqual(genericPropertySpec.PropertyType, genericTypeParameterSpec);
        }

        [TestMethod]
        public void GenericInterfaceSpecHasGenericTypeParameter_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_INTERFACE}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.IsNotNull(genericTypeParameterSpec);
            Assert.IsTrue(genericTypeParameterSpec.IsGenericParameter);
        }

        [TestMethod]
        public void GenericClassSpecHasGenericInterface_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
                        
            Assert.IsTrue(genericClassSpec.Interfaces.Any());
            Assert.IsTrue(genericClassSpec.Interfaces.All(i => !i.IsMissingSpec));
            Assert.IsTrue(genericClassSpec.Interfaces.All(i => !i.IsNullSpec));
        }

        [TestMethod]
        public void GenericInterfaceImplementationHasOwnSpec_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
            var genericInterfaceSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_INTERFACE}");

            var genericInterfaceImplementations = genericClassSpec.Interfaces.OfType<GenericInstanceSpec>()
                .Where(g => g.InstanceOf == genericInterfaceSpec);

            Assert.IsTrue(genericInterfaceImplementations.Any());
            Assert.IsTrue(genericInterfaceImplementations.All(g => !g.IsMissingSpec));
            Assert.IsTrue(genericInterfaceImplementations.All(g => !g.IsNullSpec));
        }

        [TestMethod]
        public void ClassImplementingGenericInterfaceCreatesGenericInstanceSpec_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_INTERFACE}");
            var genericInstance = genericClassSpec.Interfaces.OfType<GenericInstanceSpec>().SingleOrDefault();

            Assert.IsNotNull(genericInstance);
            Assert.IsFalse(genericInstance.IsMissingSpec);
            Assert.IsFalse(genericInstance.IsNullSpec);
        }

        [TestMethod]
        public void GenericInstanceSpecIsInstanceOfGenericInterface_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_INTERFACE}");
            var genericInstance = genericClassSpec.Interfaces.OfType<GenericInstanceSpec>().SingleOrDefault();
            var genericInterface = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_INTERFACE}") as GenericTypeSpec;
            genericInstance.ForceRebuildSpec();
            Assert.IsNotNull(genericInterface);
            Assert.IsTrue(genericInterface.GenericInstances.Contains(genericInstance));
            Assert.IsFalse(genericInstance.IsMissingSpec);
            Assert.IsFalse(genericInstance.IsNullSpec);
        }

        [TestMethod]
        public void GenericInstanceShouldAlwaysHaveSameSpec_Test()
        {
            //e.g.
            //IEnumerable<int> as a ReturnType should be the EXACT SAME SPEC as IEnumerable<int> used as a PropertyType or ParameterType
            //
            //var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS}");
            //var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            //Assert.AreEqual(_basicClassSpec, genericTypeParameterSpec.BaseSpec);
        }

        [TestMethod]
        public void GenericClassSpecHasGenericTypeConstraints_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.AreEqual(_basicClassSpec, genericTypeParameterSpec.BaseSpec);
            Assert.IsFalse(_basicClassSpec.IsMissingSpec);
            Assert.IsFalse(_basicClassSpec.IsNullSpec);
        }

        [TestMethod]
        public void GenericClassSpecHasDefaultConstructorTypeConstraints_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_DEFAULT_CONSTRUCTOR_TYPE_CONSTRAINTS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.IsTrue(genericTypeParameterSpec.HasDefaultConstructorConstraint);
        }


        [TestMethod]
        public void GenericClassSpecHasParameterlessConstructorTypeConstraints_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.AreEqual(_basicClassSpec, genericTypeParameterSpec.BaseSpec);
        }

        #endregion

        #region Generic Method Tests

        [TestMethod]
        public void MethodHasGenericTypeParameter_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericReturnType") as GenericMethodSpec;
            
            Assert.IsNotNull(genericMethod);
            Assert.IsTrue(genericMethod.GenericTypeParameters.Any());

            Assert.IsNotNull(genericMethod.GenericTypeParameters.SingleOrDefault());
            Assert.IsTrue(genericMethod.GenericTypeParameters.All(i => !i.IsMissingSpec));
        }

        [TestMethod]
        public void MethodHasGenericTypeInstanceAsReturnType_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericTypeInstanceAsReturnType");
            var returnType = genericMethod.ReturnType as GenericInstanceSpec;

            Assert.IsNotNull(genericMethod);
            Assert.IsTrue(returnType.IsGenericInstance);
        }

        [TestMethod]
        public void GenericMethodReturnTypeMatchesGenericTypeParameter_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericReturnType") as GenericMethodSpec;
            var genericTypeArgument = genericMethod.GenericTypeParameters.SingleOrDefault();


            Assert.IsNotNull(genericMethod.ReturnType);
            Assert.AreEqual(genericMethod.ReturnType, genericTypeArgument);
            Assert.IsFalse(genericMethod.ReturnType.IsMissingSpec);
            Assert.IsFalse(genericMethod.ReturnType.IsNullSpec);
        }

        [TestMethod]
        public void GenericMethodHasGenericTypeConstraints_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericTypeConstraints") as GenericMethodSpec;

            Assert.IsNotNull(genericMethod);
            Assert.IsTrue(genericMethod.GenericTypeParameters.Any());
            Assert.IsNotNull(genericMethod.GenericTypeParameters.SingleOrDefault());
        }

        [TestMethod]
        public void GenericMethodParameterMatchesGenericType_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericParameter") as GenericMethodSpec;
            var genericTypeArgument = genericMethod.GenericTypeParameters.SingleOrDefault();

            Assert.IsNotNull(genericMethod);
            Assert.AreEqual(genericMethod.Parameters.Single().ParameterType, genericTypeArgument);
        }

        [TestMethod]
        public void GenericMethodHasMultipleParameterSpecs_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithMultipleGenericTypeArguments") as GenericMethodSpec;
            var genericTypeArguments = genericMethod.GenericTypeParameters;

            Assert.IsTrue(genericTypeArguments.Count() > 1);
            
        }

        [TestMethod]
        public void GenericMethodHasNestedGenericTypeSpecs_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithNestedGenericType") as GenericMethodSpec;
            var genericTypeArgument = genericMethod.GenericTypeParameters.SingleOrDefault();

            var returnType = genericMethod.ReturnType as GenericInstanceSpec;
            returnType.ForceRebuildSpec();
            var returnTypeGenericTypeArgument = returnType.GenericTypeArguments.SingleOrDefault();

            Assert.AreEqual(returnTypeGenericTypeArgument, genericTypeArgument);

        }

        [TestMethod]
        public void SynonymousGenericMethodsHaveDistinctSpecs_Test()
        {
            var classWithSynonymousMethods = _moduleSpec
                .GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_SYNONYMOUS_GENERIC_METHODS}");

            var interfaceWithSynonymousMethods = classWithSynonymousMethods.Interfaces.Single();

            Assert.IsNotNull(interfaceWithSynonymousMethods);

            foreach (var method in interfaceWithSynonymousMethods.Methods)
            {
                Assert.IsNotNull(classWithSynonymousMethods.FindMatchingMethodSpec(method));
            }

        }
        #endregion

    }
}