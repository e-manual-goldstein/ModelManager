using Microsoft.Extensions.Logging;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyAnalyser
{
    public abstract class AbstractSpec : ISpec
    {
        private bool _analysing;
        private bool _analysed;
        private bool _processing;
        private bool _processed;
        private bool? _included;
        protected bool Included => _included ??= !IsExcluded() && IsIncluded();
        protected ISpecManager _specManager;
        public ILogger Logger { get; internal set; }

        public AbstractSpec(List<IRule> rules, ISpecManager specManager)
        {
            _specManager = specManager;
            InclusionRules = rules.OfType<InclusionRule>().ToList();
            ExclusionRules = rules.OfType<ExclusionRule>().ToList();            
        }

        protected TypeSpec[] _attributes;
        public TypeSpec[] Attributes => _attributes ??= _specManager.TryLoadAttributeSpecs(() => GetAttributes(), this);

        protected abstract CustomAttribute[] GetAttributes();

        public void Process()
        {
            if (ShouldProcess())
            {
                Build();
            }
        }

        public bool IsProcessed => _processed;

        protected abstract void BuildSpec();

        protected void Build()
        {
            _processing = true;
            BuildSpec();
            _processed = true;
        }

        private bool ShouldProcess()
        {
            return !_processing && !_processed;
        }

        public bool IsExcluded()
        {
            return ExclusionRules.Any(r => r.Exclude(this));
        }

        public bool IsIncluded()
        {
            return InclusionRules.All(r => r.Include(this));
        }

        public string ExcludedReason { get; set; }

        public void Exclude(string excludedReason)
        {
            ExcludedReason = excludedReason;    
            ExclusionRules.Add(new ExclusionRule(s => true));
        }
        
        public bool Skipped { get; private set; }
        public string SkipReason { get; set; }
        public void SkipProcessing(string skipReason)
        {
            SkipReason = skipReason;
            Skipped = true;
            _processed = true;
        }

        public List<ExclusionRule> ExclusionRules { get; private set; }
        public List<InclusionRule> InclusionRules { get; private set; }
    }
}
