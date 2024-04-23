// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Extensions;
using lg2de.SimpleAccounting.Infrastructure;
using Microsoft.Xaml.Behaviors.Core;
using Screen = Caliburn.Micro.Screen;

/// <summary>
///     Implements the view model to visualize an application error.
/// </summary>
internal class ErrorMessageViewModel : Screen
{
    private readonly IProcess processApi;

    public ErrorMessageViewModel(IProcess processApi)
    {
        this.processApi = processApi;
    }

    public string Introduction { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string FullErrorText => this.Introduction + Environment.NewLine + this.ErrorMessage;

    public string CallStack { get; init; } = string.Empty;

    public ICommand ReportGitHubCommand => new ActionCommand(this.OnReportToGitHub);

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
        string errorReportText = this.CreateErrorReportText();
        Uri uri = Defines.FormatEmailUri(errorReportText);
        this.processApi.ShellExecute(uri.AbsoluteUri);
    }

    private void OnCopyToClipboard()
    {
        string errorReportText = this.CreateErrorReportText();
        Clipboard.SetText(errorReportText);
    }

    private string CreateErrorReportText()
    {
        var builder = new StringBuilder(this.ErrorMessage);
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(this.CallStack))
        {
            builder.AppendLine(this.CallStack);
            builder.AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine($"Version: {this.GetType().GetInformationalVersion()}");
        string errorReportText = builder.ToString();
        return errorReportText;
    }
}
