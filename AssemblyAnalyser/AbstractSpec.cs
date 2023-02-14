using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public TypeSpec[] Attributes { get; protected set; }

        public void Process()
        {
            if (ShouldProcess())
            {
                Build();
            }
        }

        public async Task AnalyseAsync(Analyser analyser)
        {
            await BeginAnalysisBase(analyser);
        }

        protected abstract Task BeginAnalysis(Analyser analyser);

        protected async Task BeginAnalysisBase(Analyser analyser)
        {
            if (ShouldAnalyse())
            {
                _analysing = true;
                await BeginAnalysis(analyser);
                _analysed = true;
            }
            else
            {
                Logger.LogWarning(SkipReason);
            }
        }

        public bool Analysed => _analysed;

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

        private bool ShouldAnalyse()
        {
            return Included && _processed && !_analysed && !_analysing;
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
