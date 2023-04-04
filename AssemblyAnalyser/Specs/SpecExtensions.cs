
namespace AssemblyAnalyser
{
    public static class SpecExtensions
    {
        public static bool HasExactParameters(this IHasParameters hasParameters, ParameterSpec[] parameterSpecs)
        {
            if (parameterSpecs.Length == hasParameters.Parameters.Length)
            {
                for (int i = 0; i < parameterSpecs.Length; i++)
                {
                    if (hasParameters.Parameters[i].ParameterType != parameterSpecs[i].ParameterType
                        || hasParameters.Parameters[i].IsOut != parameterSpecs[i].IsOut
                        || hasParameters.Parameters[i].IsParams != parameterSpecs[i].IsParams)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
