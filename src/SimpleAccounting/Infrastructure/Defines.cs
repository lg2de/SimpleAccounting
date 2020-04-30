// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    internal static class Defines
    {
        public const string ProjectName = "SimpleAccounting";
        public const string GithubDomain = "github.com";
        public const string OrganizationName = "lg2de";

        public static readonly string ProjectUrl = $"https://{GithubDomain}/{OrganizationName}/{ProjectName}";
        public static readonly string NewIssueUrl = $"{ProjectUrl}/issues/new?template=bug-report.md";
    }
}
