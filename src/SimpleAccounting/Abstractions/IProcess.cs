// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System.Diagnostics;

    /// <summary>
    ///     Abstracts the API to handled .NET processes.
    /// </summary>
    internal interface IProcess
    {
        Process GetProcessByName(string processName);

        Process GetCurrentProcess();

        int GetCurrentProcessId();

        Process Start(ProcessStartInfo info);

        bool IsProcessWindowVisible(Process process);

        void BringProcessToFront(Process process);

        void MinimizeProcess(Process process);
    }
}
