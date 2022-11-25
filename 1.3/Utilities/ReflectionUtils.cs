using System.Diagnostics;
using System.Reflection;

namespace StorageFilters.Utilities
{
    internal static class ReflectionUtils
    {
        internal static bool IsMethodInCallStack(MethodBase method)
        {
            StackFrame[] stackFrames = new StackTrace().GetFrames();
            // 0 = current method
            // 1 = calling method
            // 2+ = methods we want to check
            if (stackFrames.Length > 2)
                for (int i = 2; i <= stackFrames.Length; i++)
                {
                    StackFrame stackFrame = stackFrames[i];
                    if (stackFrame is null)
                    {
                        continue;
                    }

                    MethodBase stackMethod = stackFrame.GetMethod();
                    if (stackMethod is null)
                    {
                        continue;
                    }
                    if (stackMethod == method)
                    {
                        return true;
                    }
                }
            return false;
        }
    }
}
