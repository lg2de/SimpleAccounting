// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

using System;
using System.Globalization;
using System.Text.Encodings.Web;

/// <summary>
///     Implements several constant and static texts.
/// </summary>
internal static class Defines
{
    public const string ProjectName = "SimpleAccounting";
    public const string OrganizationName = "lg2de";
    private const string GithubDomain = "github.com";

    public const string ProjectUrl = $"https://{GithubDomain}/{OrganizationName}/{ProjectName}";
    public const string NewBugUrl = $"{ProjectUrl}/issues/new?template=bug-report.md";
    private const string NewIssueUrlTemplate = $"{ProjectUrl}/issues/new?body={{0}}";
    public const string AutoSaveFileSuffix = "~";

    public static string GetAutoSaveFileName(string fileName) => fileName + AutoSaveFileSuffix;

    public static string GetReservationFileName(string fileName) => fileName + "#";

    public static Uri FormatNewIssueUrl(string bodyText)
    {
        var convertedText = UrlEncoder.Default.Encode(bodyText);
        return new Uri(string.Format(CultureInfo.CurrentUICulture, NewIssueUrlTemplate, convertedText));
    }
}
