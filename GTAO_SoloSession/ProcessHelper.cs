using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GTAO_SoloSession
{
    internal static class ProcessHelper
    {
        public static void SuspendProcess(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr hThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (hThread == IntPtr.Zero) continue;

                try { SuspendThread(hThread); }
                finally { CloseHandle(hThread); }
            }
        }

        public static void ResumeProcess(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr hThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (hThread == IntPtr.Zero) continue;

                try { while (ResumeThread(hThread) > 0) { } }
                finally { CloseHandle(hThread); }
            }
        }

        [Flags]
        private enum ThreadAccess : int
        {
            SUSPEND_RESUME = 0x0002
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccess access, bool inheritHandle, uint threadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
