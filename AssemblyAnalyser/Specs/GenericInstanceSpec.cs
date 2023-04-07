﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    public class GenericInstanceSpec : TypeSpec
    {
        GenericInstanceType _genericInstance;

        public GenericInstanceSpec(GenericInstanceType genericInstance, ISpecManager specManager)
            : base($"{genericInstance.Namespace}.{genericInstance.Name}", genericInstance.FullName, specManager)
        {
            _genericInstance = genericInstance;
            Name = _genericInstance.Name;
        }

        public override bool IsGenericInstance => true;

        protected override void BuildSpec()
        {
            if (FullTypeName != null && _genericInstance != null)
            {
                BuildSpecInternal();
            }
            else
            {
                _specManager.AddFault(FaultSeverity.Error, "Cannot build Spec with null FullTypeName");
            }
            _instanceOf = TryGetInstanceOfType();
            _genericTypeArguments = TryGetGenericTypeArguments();
        }

        TypeSpec _instanceOf;
        public TypeSpec InstanceOf => _instanceOf ??= TryGetInstanceOfType();

        private TypeSpec TryGetInstanceOfType()
        {
            if (_specManager.TryLoadTypeSpec(() => _genericInstance.ElementType, out TypeSpec typeSpec))
            {
                if (typeSpec is GenericTypeSpec genericType)
                {
                    genericType.RegisterAsInstanceOfGenericType(this);
                }
            }
            return typeSpec;
        }

        //Is this really necessary?
        protected override TypeSpec CreateBaseSpec()
        {
            if (_specManager.TryLoadTypeSpec(() => _genericInstance.ElementType, out TypeSpec typeSpec))
            {
                if (!typeSpec.IsNullSpec)
                {
                    typeSpec.AddSubType(this);
                }
            }
            return typeSpec;
        }

        protected override TypeSpec[] CreateInterfaceSpecs()
        {
            return InstanceOf.Interfaces;
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
            _specManager.TryLoadTypeSpecs(() => _genericInstance.GenericArguments.ToArray(), out TypeSpec[] typeSpecs);
            return typeSpecs;
        }

        protected override ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByScopeName(_genericInstance.Module, _genericInstance.Scope);
        }

        protected override TypeSpec[] CreateGenericTypeParameters()
        {
            _specManager.TryLoadTypeSpecs(() => _genericInstance.GenericParameters.ToArray(), out TypeSpec[] typeSpecs);
            return typeSpecs;
        }
    }
}
