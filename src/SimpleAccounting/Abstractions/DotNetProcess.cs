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
internal partial class DotNetProcess : IProcess
{
    public Process? GetProcessByName(string processName)
    {
        return Array.Find(
            Process.GetProcesses(),
            x => x.ProcessName.Equals(processName, StringComparison.InvariantCultureIgnoreCase));
    }

    public Process GetCurrentProcess()
    {
        return Process.GetCurrentProcess();
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
    private static partial class WinApi
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

        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr handle);

        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr handle, int nCmdShow);

        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr handle);
    }
}
