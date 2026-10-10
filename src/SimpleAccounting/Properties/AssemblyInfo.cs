// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// setting "english" as default language
[assembly: NeutralResourcesLanguage("en")]

[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]

// unit tests
[assembly: InternalsVisibleTo("SimpleAccounting.UnitTests")]
[assembly: InternalsVisibleTo("SimpleAccounting.IntegrationTests")]

// for NSubstitute using code generator
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
