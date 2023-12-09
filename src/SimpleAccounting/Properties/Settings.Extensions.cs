// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Properties;

internal sealed partial class Settings
{
    private const int MaxRecentProjects = 10;

    /// <summary>
    ///     Sets the specified project file (path) as the most recent project. 
    /// </summary>
    /// <remarks>
    ///     The list of recent projects is updated automatically.
    /// </remarks>
    /// <param name="projectFileName">The full path of the recent project.</param>
    public void SetRecentProject(string projectFileName)
    {
        this.RecentProjects ??= [];
        this.RecentProjects.Remove(projectFileName);
        this.RecentProjects.Insert(0, projectFileName);
        while (this.RecentProjects.Count > MaxRecentProjects)
        {
            this.RecentProjects.RemoveAt(MaxRecentProjects);
        }
    }
}
