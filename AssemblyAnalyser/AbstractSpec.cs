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
        public ILogger Logger { get; internal set; }


        public AbstractSpec(List<IRule> rules) 
        {
            InclusionRules = rules.OfType<InclusionRule>().ToList();
            ExclusionRules = rules.OfType<ExclusionRule>().ToList();
        }

        public void Process(Analyser analyser)
        {
            if (ShouldProcess())
            {
                BeginProcessingBase(analyser);                
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
        }

        public bool Analysed => _analysed;

        protected abstract void BeginProcessing(Analyser analyser);
        protected void BeginProcessingBase(Analyser analyser)
        {
            _processing = true;
            BeginProcessing(analyser);
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

        public void Exclude()
        {
            ExclusionRules.Add(new ExclusionRule(s => true));
        }
        
        public bool Skipped { get; private set; }

        public void SkipProcessing()
        {
            Skipped = true;
            _processed = true;
        }

        public List<ExclusionRule> ExclusionRules { get; private set; }
        public List<InclusionRule> InclusionRules { get; private set; }
    }
}
