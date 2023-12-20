// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
///     Implements <see cref="IHttpClient"/> using default framework implementations.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DotNetHttpClient : IHttpClient, IDisposable
{
    private readonly HttpClient httpClient = new();

    public void Dispose()
    {
        this.httpClient.Dispose();
    }

    public Task<string> GetStringAsync(Uri uri)
    {
        return this.httpClient.GetStringAsync(uri);
    }
}
