// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure
{
    using System.Threading.Tasks;

    internal interface IApplicationUpdate
    {
        Task<bool> IsUpdateAvailableAsync();

        void StartUpdateProcess();
    }
}
