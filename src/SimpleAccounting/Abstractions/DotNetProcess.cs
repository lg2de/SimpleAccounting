// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

/// <summary>
///     Implements <see cref="IProcess"/> using default framework implementations.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "The abstraction is for unit testing only. This is the simple implementation.")]
[UsedImplicitly]
internal class DotNetProcess : IProcess
{
    public Process? GetProcessByName(string processName)
    {
        return Process.GetProcesses().FirstOrDefault(
            x => x.ProcessName.Equals(processName, StringComparison.InvariantCultureIgnoreCase));
    }

    public Process GetCurrentProcess()
    {
        return Process.GetCurrentProcess();
    }

    public int GetCurrentProcessId()
    {
        return Process.GetCurrentProcess().Id;
    }

    public Process? Start(ProcessStartInfo info)
    {
        return Process.Start(info);
    }

    public bool IsProcessWindowVisible(Process process)
    {
        if (process.HasExited)
        {
            return false;
        }

        if (!process.MainWindowHandle.Equals(IntPtr.Zero))
        {
            // is visible
            return true;
        }

        // check child processes
        var childProcesses = Process.GetProcesses().Where(
            p =>
                p.ProcessName.Equals(process.ProcessName, StringComparison.OrdinalIgnoreCase)
                && p.Id != process.Id);
        return childProcesses.Select(this.IsProcessWindowVisible).FirstOrDefault();
    }

    public void BringProcessToFront(Process process)
    {
        WinApi.BringProcessToFront(process);
    }

    public void MinimizeProcess(Process process)
    {
        WinApi.MinimizeProcess(process);
    }

    public void ShellExecute(string fileName)
    {
        Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }

    [ExcludeFromCodeCoverage(Justification = "The WinAPI cannot be tested.")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Names are following external API.")]
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
