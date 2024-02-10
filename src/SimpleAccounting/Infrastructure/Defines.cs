// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure;

/// <summary>
///     Implements several constant and static texts.
/// </summary>
internal static class Defines
{
    public const string ProjectName = "SimpleAccounting";
    public const string OrganizationName = "lg2de";
    private const string GithubDomain = "github.com";

    public const string ProjectUrl = $"https://{GithubDomain}/{OrganizationName}/{ProjectName}";
    public const string NewIssueUrl = $"{ProjectUrl}/issues/new?template=bug-report.md";

    public static string GetAutoSaveFileName(string fileName) => fileName + "~";

    public static string GetReservationFileName(string fileName) => fileName + "#";
}
