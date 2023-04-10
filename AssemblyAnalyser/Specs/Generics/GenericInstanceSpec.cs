using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser
{
    public class GenericInstanceSpec : TypeSpec
    {
        GenericInstanceType _genericInstance;

        public GenericInstanceSpec(GenericInstanceType genericInstance, string fullTypeName, ISpecManager specManager)
            : base($"{genericInstance.Namespace}.{genericInstance.FullName}", fullTypeName, specManager)
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

        private TypeSpec TryGetInstanceOfType()
        {
            if (!_specManager.TryLoadTypeSpec(() => _genericInstance.ElementType, out TypeSpec typeSpec))
            {
                _specManager.AddFault(FaultSeverity.Error, $"Could not load Instance");
            }
            if (typeSpec is GenericTypeSpec genericType)
            {
                genericType.RegisterAsInstanceOfGenericType(this);
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
            _specManager.TryLoadTypeSpecs(() => _genericInstance.GenericArguments.ToArray(), out TypeSpec[] typeSpecs);
            return typeSpecs;
        }

        protected override ModuleSpec TryGetModule()
        {
            return _specManager.LoadReferencedModuleByScopeName(_genericInstance.Module, _genericInstance.Scope);
        }

        protected override GenericParameterSpec[] CreateGenericTypeParameters()
        {
            _specManager.TryLoadTypeSpecs(() => _genericInstance.GenericParameters.ToArray(), out GenericParameterSpec[] typeSpecs);
            return typeSpecs;
        }

        protected override void ProcessInterfaceImplementations()
        {
            //No processing required for instances of Generic Types
        }
    }
}
