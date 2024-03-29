﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Implements view to create or edit accounts.
/// </summary>
public partial class AccountView
{
    [ExcludeFromCodeCoverage(Justification = "The view class will not be tested.")]
    public AccountView()
    {
        this.InitializeComponent();
    }
}
