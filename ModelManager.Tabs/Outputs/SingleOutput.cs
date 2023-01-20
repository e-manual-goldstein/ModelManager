﻿using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelManager.Tabs.Outputs
{
    public class SingleOutput : AbstractOutput<string>
    {
        public SingleOutput(string content) : base(content)
        {
            
        }

        public override OutputType OutputType => OutputType.Single;
    }
}
