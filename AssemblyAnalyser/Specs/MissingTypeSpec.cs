using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyAnalyser.Specs
{
    internal class MissingTypeSpec : TypeSpec
    {
        public MissingTypeSpec(string fullTypeName, string uniqueTypeName, ISpecManager specManager) 
            : base(fullTypeName, uniqueTypeName, specManager)
        {
        }

        public override void AddImplementation(TypeSpec typeSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");
        }

        public override void RegisterAsDependentParameterSpec(ParameterSpec parameterSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");
        }

        public override void RegisterAsResultType(IMemberSpec methodSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");
        }

        public override void RegisterAsDecorator(AbstractSpec decoratedSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");
        }

        public override void RegisterAsDelegateFor(EventSpec eventSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");            
        }

        public override void RegisterDependentMethodSpec(MethodSpec methodSpec)
        {
            _specManager.AddFault(FaultSeverity.Error, $"Missing TypeSpec for {UniqueTypeName}");            
        }
    }
}
