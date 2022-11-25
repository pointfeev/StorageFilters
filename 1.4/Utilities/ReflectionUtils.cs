using System.Diagnostics;
using System.Reflection;

namespace StorageFilters.Utilities
{
    internal static class ReflectionUtils
    {
        internal static bool IsMethodInCallStack(MethodBase method)
        {
            StackFrame[] stackFrames = new StackTrace().GetFrames();
            if (stackFrames.Length > 2)
                for (int i = 2; i < stackFrames.Length; i++)
                    if (stackFrames[i].GetMethod() == method)
                        return true;
            return false;
        }
    }
}
