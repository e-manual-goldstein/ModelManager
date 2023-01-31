using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public abstract class AbstractOutput<T> : IOutput
    {
        protected AbstractOutput(T content) 
        { 
            Content = content;
            ContentActions = new Dictionary<string, Func<T, IOutput>>();
        }

        public T Content { get; private set; }

        public abstract OutputType OutputType { get; }

        public Dictionary<string, Func<T, IOutput>> ContentActions { get; }
    }
}
