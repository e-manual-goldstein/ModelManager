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
            _actionContextsById.Add(GetNewActionId($"{_actionMethod.DeclaringType}.{_actionMethod.Name}"), this);
        }

        public bool HasParameters => _actionMethod.GetParameters().Any();

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

        private static Dictionary<string, ActionContext> _actionContextsById = new Dictionary<string, ActionContext>();

        private static Dictionary<string, int> _actionIds = new Dictionary<string, int>();

        public static ActionContext GetCurrentContextByActionName(string actionName)
        {
            return GetContextByActionId(GetCurrentActionId(actionName));
        }

        private static ActionContext GetContextByActionId(string actionContextId)
        {
            return _actionContextsById[actionContextId];
        }

        private static string GetCurrentActionId(string actionName)
        {
            return $"{actionName}_{_actionIds[actionName]}";
        }

        private static string GetNewActionId(string baseActionName)
        {
            
            if (!_actionIds.ContainsKey(baseActionName))
            {
                _actionIds.Add(baseActionName, 0);
            }
            var index = ++_actionIds[baseActionName];
            return $"{baseActionName}_{index}";
        }

        public void UpdateProgress(double current, double max)
        {
            _tab.UpdateProgress(current, max);
        }
    }
}
