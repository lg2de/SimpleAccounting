// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.InteropServices;

    [ExcludeFromCodeCoverage]
    internal class DotNetProcess : IProcess
    {
        public Process GetProcessByName(string processName)
        {
            return Process.GetProcesses().FirstOrDefault(
                x => x.ProcessName.Equals(processName, StringComparison.InvariantCultureIgnoreCase));
        }

        public Process GetCurrentProcess()
        {
            return Process.GetCurrentProcess();
        }

        public void BringProcessToFront(Process process)
        {
            WinApi.BringProcessToFront(process);
        }

        public void MinimizeProcess(Process process)
        {
            WinApi.MinimizeProcess(process);
        }

        [ExcludeFromCodeCoverage]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static class WinApi
        {
            private const int SW_MINIMIZE = 6;
            private const int SW_RESTORE = 9;

            public static void BringProcessToFront(Process process)
            {
                IntPtr handle = process.MainWindowHandle;
                if (IsIconic(handle))
                {
                    ShowWindow(handle, SW_RESTORE);
                }

                SetForegroundWindow(handle);
            }

            public static void MinimizeProcess(Process process)
            {
                IntPtr handle = process.MainWindowHandle;
                ShowWindow(handle, SW_MINIMIZE);
            }

            [DllImport("User32.dll")]
            private static extern bool SetForegroundWindow(IntPtr handle);

            [DllImport("User32.dll")]
            private static extern bool ShowWindow(IntPtr handle, int nCmdShow);

            [DllImport("User32.dll")]
            private static extern bool IsIconic(IntPtr handle);
        }
    }
}
