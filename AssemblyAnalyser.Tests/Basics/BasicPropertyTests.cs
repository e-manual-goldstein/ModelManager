using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net.Cache;

namespace AssemblyAnalyser.Tests
{
    [TestClass]
    public class BasicPropertyTests : AbstractSpecTests
    {
        ModuleSpec _vbModuleSpec;
        TypeSpec _basicVBClassSpec;
        TypeSpec _basicVBInterfaceSpec;
        TypeSpec _basicVBSubClassSpec;
        TypeSpec _basicSubClassSpec;
        TypeSpec _basicGrandchildClassSpec;

        [TestInitialize] 
        public override void Initialize() 
        {
            base.Initialize();
            var vbFilePath = "..\\..\\..\\..\\AssemblyAnalyser.VBTestData\\bin\\Debug\\net35\\AssemblyAnalyser.VBTestData.dll";
            _vbModuleSpec = _specManager.LoadAssemblySpecFromPath(Path.GetFullPath(vbFilePath)).LoadModuleSpecFromPath(Path.GetFullPath(vbFilePath)); 
            _vbModuleSpec.Process();
            _basicVBClassSpec = _vbModuleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.VBTestData.Basics.BasicVBClass");
            _basicVBSubClassSpec = _vbModuleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.VBTestData.Basics.BasicVBSubClass");
            _basicSubClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicSubClass");
            _basicGrandchildClassSpec = _moduleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.TestData.Basics.BasicGrandchildClass");
            _basicVBInterfaceSpec = _vbModuleSpec.TypeSpecs
                .Single(d => d.FullTypeName == "AssemblyAnalyser.VBTestData.Basics.IBasicVBInterface");
        }

        #region Basic Property Tests
        
        [TestMethod]
        public void BasicPropertySpecIsNotNull_Test()
        {
            Assert.IsNotNull(_basicClassSpec.GetPropertySpec("PublicProperty"));
        }

        [TestMethod]
        public void BasicPropertySpecLinkedToInterfaceImplementationMember_Test()
        {
            _basicClassSpec.ForceRebuildSpec();
            var interfacePropertySpec = _basicInterfaceSpec.GetPropertySpec("ReadOnlyInterfaceImpl");
            var concretePropertySpec = _basicClassSpec.GetPropertySpec("ReadOnlyInterfaceImpl");
            concretePropertySpec.ForceRebuildSpec();
            Assert.IsTrue(concretePropertySpec.ImplementationFor.Any());
            Assert.IsTrue(concretePropertySpec.ImplementationFor.Contains(interfacePropertySpec));
            
        }

        [TestMethod]
        public void ExplicitlyImplementedPropertyLinkedToInterface_Test()
        {
            _basicClassSpec.ForceRebuildSpec();
            var interfaceProperty = _basicInterfaceSpec.GetPropertySpec("PropertyForExplicitImplementation");
            interfaceProperty.ForceRebuildSpec();
            var interfaceImplementation = _basicClassSpec.GetPropertySpec(interfaceProperty.ExplicitName);
            interfaceImplementation.ForceRebuildSpec();

            Assert.IsTrue(interfaceImplementation.ImplementationFor.Any());
            Assert.IsTrue(interfaceImplementation.ImplementationFor.Contains(interfaceProperty));
        }

        [TestMethod]
        public void BasicPropertyWithArrayTypeHasOwnSpec_Test()
        {
            var stringProperty = _basicClassSpec.GetPropertySpec("PublicProperty");
            var stringArrayProperty = _basicClassSpec.GetPropertySpec("ArrayProperty");

            var stringSpec = stringProperty.PropertyType;
            var stringArraySpec = stringArrayProperty.PropertyType;

            Assert.AreNotSame(stringSpec, stringArraySpec);
        }

        [TestMethod]
        public void BasicSubClassHasOnlyOneDeclaredProperty_Test()
        {
            Assert.AreEqual(1, _basicSubClassSpec.Properties.Count());
        }

        [TestMethod]
        public void BasicSubClassInheritsBaseProperties_Test()
        {
            var declaredProperties = _basicSubClassSpec.Properties;
            var allProperties = _basicSubClassSpec.GetAllPropertySpecs();

            Assert.IsTrue(allProperties.Intersect(declaredProperties).Any());
            Assert.IsTrue(allProperties.Except(declaredProperties).Any());
        }

        [TestMethod]
        public void InheritedPropertyRepresentedBySameSpec_Test()
        {
            var publicPropertySpecs = _specManager.PropertySpecs.Where(p => p.Name == "PropertyWithUniqueName");
            
            Assert.IsTrue(publicPropertySpecs.Any());
            Assert.AreEqual(1, publicPropertySpecs.Count());

            var publicPropertySpec = publicPropertySpecs.Single();
            
            var inheritedProperty = _basicSubClassSpec.GetPropertySpec("PropertyWithUniqueName", true);
            var baseProperty = _basicClassSpec.GetPropertySpec("PropertyWithUniqueName");

            Assert.AreEqual(inheritedProperty, publicPropertySpec);
            Assert.AreEqual(inheritedProperty, baseProperty);
            
        }

        [TestMethod]
        public void OverridingPropertyHasOwnSpec_Test()
        {
            var overridablePropertySpecs = _specManager.PropertySpecs.Where(p => p.Name == "OverridableProperty");

            Assert.IsTrue(overridablePropertySpecs.Any());
            Assert.AreEqual(2, overridablePropertySpecs.Count());

            var primary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicVBClassSpec);
            var secondary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicVBSubClassSpec);
            Assert.AreNotEqual(primary, secondary); 
        }

        [TestMethod]
        public void OverridingPropertyIsLinkedToOverriddenProperty_Test()
        {
            var overridablePropertySpecs = _specManager.PropertySpecs.Where(p => p.Name == "OverridableProperty").ToArray();
            var primary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicVBClassSpec);
            var secondary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicVBSubClassSpec);

            Assert.AreEqual(secondary.BaseSpec, primary);
        }

        [TestMethod]
        public void OverridingPropertyIsLinkedToOverriddenPropertyOnGrandparentClass_Test()
        {
            var overridablePropertySpecs = _specManager.MethodSpecs
                .Where(p => p.Name == "SecondOverridableMethod").ToArray();
            var primary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicClassSpec);
            var tertiary = overridablePropertySpecs.SingleOrDefault(p => p.DeclaringType == _basicGrandchildClassSpec);
            
            Assert.AreEqual(tertiary.BaseSpec, primary);
        }

        [TestMethod]
        //This is a test specifically for Types defined with Property members that have parameters
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicPropertyCanHaveOverloads_Test()
        {
            //Assert there are two identically named properties
            Assert.AreEqual(2, _basicVBClassSpec.Properties.Where(p => p.Name == "PropertyWithParameters").Count());
            foreach (var property in _basicVBClassSpec.Properties)
            {
                var propertySpec = _basicVBClassSpec
                    .MatchPropertySpecByNameAndParameterType(property.Name, property.Parameters);
                Assert.IsNotNull(propertySpec);
            }            
        }
        #endregion
        
        [TestMethod]
        //This is a test specifically for Types defined with Property members whose names do not match those of the implemented interfaces
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicPropertyWithAlternateNameHasOverride_Test()
        {
            var alternateNamedFunction = _basicVBClassSpec.Properties.Where(p => p.Name == "AlternateNamedProperty").Single();

            Assert.IsNotNull(alternateNamedFunction.Overrides);
            Assert.IsTrue(alternateNamedFunction.Overrides.Any());
        }

        [TestMethod]
        //This is a test specifically for Types defined with Property members whose names do not match those of the implemented interfaces
        //This feature appears to only be possible in VisualBasic and not C#
        public void BasicPropertyWithAlternateNameMatchesInterfaceImplementation_Test()
        {
            
            var alternateNamedProperty = _basicVBClassSpec.Properties.Where(p => p.Name == "AlternateNamedProperty").Single();
            alternateNamedProperty.ForceRebuildSpec();

            var interfacePropertySpec = _basicVBInterfaceSpec.GetPropertySpec("BasicProperty");

            Assert.IsTrue(alternateNamedProperty.ImplementationFor.Any());
            Assert.IsTrue(alternateNamedProperty.ImplementationFor.Contains(interfacePropertySpec));
        }
    }
}