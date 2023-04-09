
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
                    if (!hasParameters.Parameters[i].MatchesParameter(parameterSpecs[i]))
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
