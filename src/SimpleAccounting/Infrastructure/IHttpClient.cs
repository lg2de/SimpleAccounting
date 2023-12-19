// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Threading.Tasks;

internal interface IHttpClient
{
    Task<string> GetStringAsync(Uri uri);
}
