// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the designer view model for <see cref="ProjectOptionsViewModel"/>.
/// </summary>
public class ProjectOptionsDesignViewModel : ProjectOptionsViewModel
{
    public ProjectOptionsDesignViewModel() : base(new AccountingData())
    {
    }
}
