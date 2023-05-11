using AssemblyAnalyser.Specs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public abstract class AbstractSpec : ISpec
    {
        private bool _processing;
        private bool _processed;
        private bool? _included;
        protected bool Included => _included ??= !IsExcluded() && IsIncluded();
        protected readonly ISpecManager _specManager;
        protected readonly ISpecContext _specContext;

        public AbstractSpec(ISpecManager specManager, ISpecContext specContext)
        {
            _specManager = specManager;
            _specContext = specContext;
            //if (specManager != null)
            //{
            //    SpecialInclusionRules.AddRange(specManager.SpecRules.OfType<InclusionRule>());
            //    SpecialExclusionRules.AddRange(specManager.SpecRules.OfType<ExclusionRule>());
            //}
        }

        public string Name { get; protected set; }
        public virtual bool IsSystem { get; protected set; }
        protected TypeSpec[] _attributes;
        public TypeSpec[] Attributes => _attributes ??= TryLoadAttributeSpecs();

        protected abstract TypeSpec[] TryLoadAttributeSpecs();

        protected abstract CustomAttribute[] GetAttributes();

        List<ISpecDependency> _children = new List<ISpecDependency>();
        public ISpecDependency[] Children => _children.ToArray();

        public virtual void AddChild(ISpecDependency child)
        {
            _children.Add(child);
            //Module.RegisterAsRequiredBy(specDependency);
        }

        List<ISpecDependency> _parents = new List<ISpecDependency>();
        public ISpecDependency[] Parents => _parents.ToArray();

        public virtual void AddParent(ISpecDependency parent)
        {
            _parents.Add(parent);
            //Module.RegisterAsRequiredBy(specDependency);
        }

        public void Process()
        {
            if (ShouldBuild())
            {
                Build();
            }
        }

        public void ForceRebuildSpec()
        {
            Build();
        }

        public bool IsProcessed => _processed;

        protected abstract void BuildSpec();

        protected void Build()
        {
            _processing = true;
            BuildSpec();
            _processed = true;
        }

        private bool ShouldBuild()
        {
            return !_processing && !_processed;// && IsIncluded() && !IsExcluded();
        }

        #region Inclusion / Exclusion
        public bool IsExcluded()
        {
            return SpecialExclusionRules.Any(r => r.Exclude(this)) || GeneralSpecRules.Any(r => !r.IncludeSpec(this));
        }

        public bool IsIncluded()
        {
            return SpecialInclusionRules.All(r => r.Include(this)) || GeneralSpecRules.All(r => r.IncludeSpec(this));
        }

        public string ExcludedReason { get; set; }

        public void Exclude(string excludedReason)
        {
            ExcludedReason = excludedReason;
            SpecialExclusionRules.Add(new ExclusionRule(s => true));
        }

        public IRule[] GeneralSpecRules => _specManager.SpecRules;

        public List<ExclusionRule> SpecialExclusionRules { get; private set; } = new List<ExclusionRule>();
        public List<InclusionRule> SpecialInclusionRules { get; private set; } = new List<InclusionRule>();
        #endregion

        #region Skip Processing

        public bool Skipped { get; private set; }
        public string SkipReason { get; set; }
        public void SkipProcessing(string skipReason)
        {
            SkipReason = skipReason;
            Skipped = true;
            _processed = true;
        }

        #endregion
    }
}
