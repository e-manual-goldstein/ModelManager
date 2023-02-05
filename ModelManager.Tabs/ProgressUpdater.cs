using ModelManager.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ModelManager.Tabs
{
    public class ProgressUpdater : IDisposable
    {
        public ProgressUpdater(AbstractServiceTab sourceTab, string actionName = null)
        {
            actionName ??= ActionContext.GetActionId();            
        }

        //public event RoutedPropertyChangedEventHandler<double> UpdateProgress;


        public void UpdateProgress(double newProgress)
        {

        }

        public void Dispose()
        {
            
        }
    }
}
