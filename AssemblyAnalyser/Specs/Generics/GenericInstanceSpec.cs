﻿using AssemblyAnalyser.Extensions;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericInstanceSpec : TypeSpec, IHasGenericParameters
    {
        GenericInstanceType _genericInstance;

        public GenericInstanceSpec(GenericInstanceType genericInstance, string fullTypeName, ModuleSpec moduleSpec, ISpecManager specManager, ISpecContext specContext)
            : base($"{genericInstance.Namespace}.{genericInstance.FullName}", fullTypeName, moduleSpec, specManager, specContext)
        {
            _genericInstance = genericInstance;
            Name = _genericInstance.Name;
        }

        public GenericInstanceType GenericInstance => _genericInstance;

        public override bool IsGenericInstance => true;

        protected override void BuildSpec()
        {
            BuildSpecInternal();            
            _instanceOf = TryGetInstanceOfType();
            _genericTypeArguments = TryGetGenericTypeArguments();
        }

        TypeSpec _instanceOf;
        public TypeSpec InstanceOf => _instanceOf ??= TryGetInstanceOfType();

        public override bool IsInterface => InstanceOf.IsInterface;

        public override bool MatchesSpec(TypeSpec typeSpec)
        {
            return typeSpec is GenericInstanceSpec genericInstanceSpec 
                && InstanceOf.Equals(genericInstanceSpec.InstanceOf)
                && genericInstanceSpec.HasExactGenericTypeArguments(GenericTypeArguments);
        }

        public bool HasExactGenericTypeArguments(TypeSpec[] genericTypeArguments)
        {
            if (genericTypeArguments.Length == GenericTypeArguments.Length)
            {
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    if (!GenericTypeArguments[i].MatchesSpec(genericTypeArguments[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private TypeSpec TryGetInstanceOfType()
        {
            var typeSpec = _specManager.LoadTypeSpec(_genericInstance.ElementType, _specContext);
            if (typeSpec == null || typeSpec.IsNullSpec)
            {
                _specManager.AddFault(this, FaultSeverity.Error, $"Could not load Instance");
            }
            if (typeSpec is GenericTypeSpec genericType)
            {
                genericType.RegisterAsInstanceOfGenericType(this);
            }
            return typeSpec;
        }

        protected override TypeSpec CreateBaseSpec()
        {
            return _specManager.GetNullTypeSpec(_specContext);
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return InstanceOf.Interfaces;
        }

        protected override MethodSpec[] CreateMethodSpecs()
        {
            return InstanceOf.Methods;
        }

        protected override PropertySpec[] CreatePropertySpecs()
        {
            return InstanceOf.Properties;
        }

        protected override TypeSpec[] CreateNestedTypeSpecs()
        {
            return InstanceOf.NestedTypes;
        }

        protected override FieldSpec[] CreateFieldSpecs()
        {
            return InstanceOf.Fields;
        }

        protected override EventSpec[] CreateEventSpecs()
        {
            return InstanceOf.Events;
        }

        protected override TypeSpec[] CreateAttributSpecs()
        {
            return InstanceOf.Attributes;
        }

        TypeSpec[] _genericTypeArguments;
        public TypeSpec[] GenericTypeArguments => _genericTypeArguments ??= TryGetGenericTypeArguments();

        private TypeSpec[] TryGetGenericTypeArguments()
        {
            return _specManager.LoadTypeSpecs(_genericInstance.GenericArguments, _specContext).ToArray();            
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            return _specManager.LoadTypeSpecs<GenericParameterSpec>(_genericInstance.GenericParameters, _specContext)
                .ToArray();            
        }

        public override FieldSpec LoadFieldSpec(FieldReference fieldReference)
        {
            FieldSpec fieldSpec = _fieldSpecs.GetOrAdd(fieldReference.FullName, 
                (spec) => CreateGenericFieldInstanceSpec(fieldReference));
            return fieldSpec;
        }

        private FieldSpec CreateGenericFieldInstanceSpec(FieldReference fieldReference)
        {
            var genericField = GetGenericField(fieldReference);
            if (genericField == null)
            {
                _specManager.AddFault(this, FaultSeverity.Error, $"No Field found to match {fieldReference}");
            }
            return new GenericFieldInstanceSpec(genericField, this, _specManager, _specContext);
        }

        private FieldSpec GetGenericField(FieldReference fieldReference)
        {
            var fields = InstanceOf.Fields;
            var matchByName = fields.Where(f => f.Name == fieldReference.Name).ToArray();
            if (matchByName.Length > 1)
            {

            }
            return matchByName.SingleOrDefault();
        }

        protected override void CheckInterfaceImplementations()
        {
            //No processing required for instances of Generic Types
        }

        public override void AddImplementation(TypeSpec typeSpec)
        {
            if (IsInterface.Equals(false))
            {
                throw new InvalidOperationException("Cannot implement a non-interface Type");
            }
            //if (!_implementations.Contains(typeSpec))
            //{
            //    _specManager.AddFault(FaultSeverity.Debug, "Is it enough to just register implementations on a generic instance?");
            //    _implementations.Add(typeSpec);                
            //}
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            //if (!_resultTypeSpecs.Contains(methodSpec))
            //{
            //    _specManager.AddFault(FaultSeverity.Debug, "Is it enough to just register Result Types on a generic instance?");
            //    _resultTypeSpecs.Add(methodSpec);                
            //}
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            //if (!_dependentMethodBodies.Contains(methodSpec))
            //{
            //    _specManager.AddFault(FaultSeverity.Debug, "Is it enough to just register Dependent Methods on a generic instance?");
            //    _dependentMethodBodies.Add(methodSpec);                
            //}
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            //if (!_dependentParameterSpecs.Contains(parameterSpec))
            //{
            //    _specManager.AddFault(FaultSeverity.Debug, "Is it enough to just register Dependent Parameter on a generic instance?");
            //    _dependentParameterSpecs.Add(parameterSpec);                
            //}
        }

        public override void AddSubType(TypeSpec typeSpec)
        {
            //if (!_subTypes.Contains(typeSpec))
            //{
            //    _specManager.AddFault(FaultSeverity.Debug, "Is it enough to just register a Sub Type on a generic instance?");
            //    _subTypes.Add(typeSpec);                
            //}
        }

        public override MethodSpec LoadMethodSpec(MethodReference method)
        {
            _specManager.AddFault(this, FaultSeverity.Debug, "Unfinished Implementation");
            return InstanceOf.LoadMethodSpec(method);
        }

        

    }
}
