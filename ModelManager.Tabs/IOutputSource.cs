using System.Reflection;

namespace ModelManager.Core
{
    public interface IOutputSource
    {
        object InvokeAction(MethodInfo actionMethod, object[] parameters);
    }
}