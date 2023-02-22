using ModelManager.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ModelManager.Tabs
{
    public class ActionContext : IOutputSource
    {
        string _actionName;
        OutputTab _tab;
        IOutputSource _actionSource;
        Func<object> _func;

        public ActionContext(MethodInfo actionMethod, OutputTab tab, IOutputSource actionSource)
        {
            HasParameters = actionMethod.GetParameters().Any();
            _actionName = actionMethod.Name;
            _func = () =>
            {
                //Application.Current.Dispatcher.Invoke(actionMethod, new object[] {})
                return actionMethod.Invoke(_actionSource, new object[] { });// InvokeAction(_actionMethod, new object[] { });
            };
            _tab = tab;
            _actionSource = actionSource;
            _actionContextsById.Add(GetNewActionId($"{actionMethod.DeclaringType}.{actionMethod.Name}"), this);
        }

        public ActionContext(string actionName, OutputTab tab, Func<object> func)
        {
            _tab = tab;
            _actionName = actionName;
            _actionContextsById.Add(GetNewActionId(actionName), this);
            _func = func;
        }

        public bool HasParameters { get; }

        public async Task ExecuteAction()
        {
            try
            {
                //_tab.DisplayExecutingMessage();
                var task = Application.Current.Dispatcher.InvokeAsync(_func);
                await task;
                _tab.DisplayOutput(task.Result, this, _actionName);
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
