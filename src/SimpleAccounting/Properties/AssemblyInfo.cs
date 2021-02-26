// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System.Resources;
using System.Runtime.CompilerServices;

// setting "english" as default language
[assembly: NeutralResourcesLanguage("en")]

// unit tests
[assembly: InternalsVisibleTo("SimpleAccounting.UnitTests")]
[assembly: InternalsVisibleTo("SimpleAccounting.IntegrationTests")]

// for NSubstitute using code generator
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
