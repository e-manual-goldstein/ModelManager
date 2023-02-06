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
        private readonly ActionContext _actionContext;

        public ProgressUpdater(AbstractServiceTab sourceTab, string actionName = null)
        {
            if (actionName == null)
            {
                var stackTrace = new StackTrace();
                var callingAction = stackTrace.GetFrame(1).GetMethod();
                actionName = $"{callingAction.DeclaringType}.{callingAction.Name}";
            }
            _actionContext = ActionContext.GetCurrentContextByActionName(actionName);
        }

        public void UpdateProgress(double current, double max)
        {
            _actionContext.UpdateProgress(current, max);
        }

        public void Dispose()
        {
            
        }
    }
}
