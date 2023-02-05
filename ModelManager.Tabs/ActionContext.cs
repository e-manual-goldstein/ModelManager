using ModelManager.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Tabs
{
    public class ActionContext : IOutputSource
    {
        MethodInfo _actionMethod;
        OutputTab _tab;
        IOutputSource _actionSource;

        public ActionContext(MethodInfo actionMethod, OutputTab tab, IOutputSource actionSource)
        {
            _actionMethod = actionMethod;
            _tab = tab;
            _actionSource = actionSource;
        }

        private static Dictionary<string, int> _actionIds = new Dictionary<string, int>();

        public bool HasParameters { get; internal set; }

        public static string GetActionId()
        {
            var stackTrace = new StackTrace();
            var callingAction = stackTrace.GetFrame(0).GetMethod();
            var baseActionName = $"{callingAction.DeclaringType}.{callingAction.Name}";
            if (!_actionIds.ContainsKey(baseActionName))
            {
                _actionIds.Add(baseActionName, 0);
            }
            var index = _actionIds[baseActionName]++;
            return $"{baseActionName}_{index}";
        }

        public async Task ExecuteAction()
        {
            try
            {
                _tab.DisplayExecutingMessage();
                var task = Task.Run(() => InvokeAction(_actionMethod, new object[] { }));
                await task;
                _tab.DisplayOutput(task.Result, this, _actionMethod);
            }
            catch (Exception ex)
            {
                _tab.DisplayError(ex, this);
            }
        }

        public object InvokeAction(MethodInfo actionMethod, object[] parameters)
        {
            return actionMethod.Invoke(_actionSource, parameters);
        }
    }
}
