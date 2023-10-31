// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Properties;

using System.Collections.Specialized;

internal sealed partial class Settings
{
    private const int MaxRecentProjects = 10;

    public void SetRecentProject(string projectFileName)
    {
        this.RecentProjects ??= new StringCollection();
        this.RecentProjects.Remove(projectFileName);
        this.RecentProjects.Insert(0, projectFileName);
        while (this.RecentProjects.Count > MaxRecentProjects)
        {
            this.RecentProjects.RemoveAt(MaxRecentProjects);
        }
    }
}
