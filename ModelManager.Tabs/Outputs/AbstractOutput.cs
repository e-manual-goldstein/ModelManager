using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public abstract class AbstractOutput<T>
    {
        protected AbstractOutput(T content) 
        { 
            Content = content;
        }

        public T Content { get; private set; }
    }
}
