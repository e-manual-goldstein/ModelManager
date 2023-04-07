﻿using AssemblyAnalyser.Specs;
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
            _moduleSpec = _specManager.LoadModuleSpec(Path.GetFullPath(filePath));
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
        }

        [TestMethod]
        public void GenericInterfaceImplementationHasOwnSpec_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
            var genericInterfaceSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_INTERFACE}");

            var genericInterfaceImplementations = genericClassSpec.Interfaces.OfType<GenericInstanceSpec>()
                .Where(g => g.InstanceOf == genericInterfaceSpec);

            Assert.IsTrue(genericInterfaceImplementations.Any());
        }

        [TestMethod]
        public void ClassImplementingGenericInterfaceCreatesGenericInstanceSpec_Test()
        {
            var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_INTERFACE}");
            var genericInstance = genericClassSpec.Interfaces.OfType<GenericInstanceSpec>().SingleOrDefault();

            Assert.IsNotNull(genericInstance);
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
        }

        //[TestMethod] //Review Test - Is this a valid test?
        //public void GenericClassSpecImplementsGenericInterface_Test()
        //{
        //    var genericClassSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS}");
        //    var genericInterfaceSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_INTERFACE}");

        //    genericClassSpec.ForceRebuildSpec();

        //    Assert.IsTrue(genericInterfaceSpec.Implementations.Contains(genericClassSpec));

        //}

        [TestMethod]
        public void GenericClassSpecHasGenericTypeConstraints_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_CLASS_TYPE_CONSTRAINTS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault();

            Assert.AreEqual(_basicClassSpec, genericTypeParameterSpec.BaseSpec);            
        }

        [TestMethod]
        public void GenericClassSpecHasDefaultConstructorTypeConstraints_Test()
        {
            var genericTypeSpec = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{GENERIC_CLASS_WITH_DEFAULT_CONSTRUCTOR_TYPE_CONSTRAINTS}");
            var genericTypeParameterSpec = genericTypeSpec.GenericTypeParameters.SingleOrDefault() as GenericParameterSpec;

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
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericReturnType");
            
            Assert.IsNotNull(genericMethod);
            Assert.IsTrue(genericMethod.GenericTypeArguments.Any());
            Assert.IsNotNull(genericMethod.GenericTypeArguments.SingleOrDefault());
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
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericReturnType");
            var genericTypeArgument = genericMethod.GenericTypeArguments.SingleOrDefault();


            Assert.IsNotNull(genericMethod.ReturnType);
            Assert.AreEqual(genericMethod.ReturnType, genericTypeArgument);
            
        }

        [TestMethod]
        public void GenericMethodHasGenericTypeConstraints_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericTypeConstraints");

            Assert.IsNotNull(genericMethod);
            Assert.IsTrue(genericMethod.GenericTypeArguments.Any());
            Assert.IsNotNull(genericMethod.GenericTypeArguments.SingleOrDefault());
        }

        [TestMethod]
        public void GenericMethodParameterMatchesGenericType_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithGenericParameter");
            var genericTypeArgument = genericMethod.GenericTypeArguments.SingleOrDefault();

            Assert.IsNotNull(genericMethod);
            Assert.AreEqual(genericMethod.Parameters.Single().ParameterType, genericTypeArgument);
        }

        [TestMethod]
        public void GenericMethodHasMultipleParameterSpecs_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithMultipleGenericTypeArguments");
            var genericTypeArguments = genericMethod.GenericTypeArguments;

            Assert.IsTrue(genericTypeArguments.Count() > 1);
            
        }

        [TestMethod]
        public void GenericMethodHasNestedGenericTypeSpecs_Test()
        {
            var classWithGenericMethods = _moduleSpec.GetTypeSpec($"{NAMESPACE}.{CLASS_WITH_GENERIC_METHODS}");
            var genericMethod = classWithGenericMethods.Methods.SingleOrDefault(n => n.Name == "MethodWithNestedGenericType");
            var genericTypeArgument = genericMethod.GenericTypeArguments.SingleOrDefault();

            var returnType = genericMethod.ReturnType as GenericInstanceSpec;
            returnType.ForceRebuildSpec();
            var returnTypeGenericTypeArgument = returnType.GenericTypeArguments.SingleOrDefault();

            Assert.AreEqual(returnTypeGenericTypeArgument, genericTypeArgument);

        }


        #endregion

    }
}