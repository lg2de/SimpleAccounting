// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Text;
using System.Windows.Input;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using Microsoft.Xaml.Behaviors.Core;
using Screen = Caliburn.Micro.Screen;

internal class ErrorMessageViewModel : Screen
{
    private readonly IProcess processApi;

    public ErrorMessageViewModel(IProcess processApi)
    {
        this.processApi = processApi;
    }

    public string ErrorText { get; init; } = string.Empty;

    public ICommand ReportGitGubCommand => new ActionCommand(this.OnReportToGitHub);

    public ICommand ReportEmailCommand => new ActionCommand(this.OnSendByEmail);

    public ICommand ClipboardCommand => new ActionCommand(this.OnCopyToClipboard);

    private void OnReportToGitHub()
    {
        string errorReportText = this.CreateErrorReportText();
        Uri uri = Defines.FormatNewIssueUrl(errorReportText);
        this.processApi.ShellExecute(uri.AbsoluteUri);
    }

    private void OnSendByEmail()
    {
    }

    private void OnCopyToClipboard()
    {
    }

    private string CreateErrorReportText()
    {
        var builder = new StringBuilder(this.ErrorText);
        builder.AppendLine();
        builder.AppendLine($"Version: {this.GetType().GetInformationalVersion()}");
        string errorReportText = builder.ToString();
        return errorReportText;
    }
}
