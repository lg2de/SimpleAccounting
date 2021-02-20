// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Infrastructure
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    internal class ProjectFileLoader
    {
        private const int MaxRecentProjects = 10;
        private readonly IFileSystem fileSystem;

        private readonly IDialogs dialogs;
        private readonly IProcess processApi;
        private readonly Settings settings;

        public ProjectFileLoader(Settings settings, IDialogs dialogs, IFileSystem fileSystem, IProcess processApi)
        {
            this.settings = settings;
            this.dialogs = dialogs;
            this.fileSystem = fileSystem;
            this.processApi = processApi;

            this.settings.RecentProjects ??= new StringCollection();
            this.settings.SecuredDrives ??= new StringCollection();
        }

        public AccountingData ProjectData { get; private set; } = new AccountingData();

        /// <summary>
        ///     Gets a value indicating whether project has been migrated.
        /// </summary>
        /// <remarks>
        ///     "Migration" covers conversion from old format and also recovering from auto save file.
        /// </remarks>
        public bool Migrated { get; private set; }

        public async Task<OperationResult> LoadAsync(string projectFileName)
        {
            try
            {
                if (!await this.CheckSecureDriveAsync(projectFileName))
                {
                    return OperationResult.Aborted;
                }

                if (!this.LoadFile(projectFileName, out bool autoSaveFileLoaded))
                {
                    return OperationResult.Failed;
                }

                if (this.ProjectData.Migrate() || autoSaveFileLoaded)
                {
                    this.Migrated = true;
                }

                this.UpdateSettings(projectFileName);

                return OperationResult.Completed;
            }
            catch (InvalidOperationException e)
            {
                string message =
                    string.Format(CultureInfo.CurrentUICulture, Resources.Information_FailedToLoadX, projectFileName)
                    + $"\n{e.Message}";
                this.dialogs.ShowMessageBox(message, Resources.Header_LoadProject);
                return OperationResult.Failed;
            }
        }

        private async Task<bool> CheckSecureDriveAsync(string projectFileName)
        {
            if (this.fileSystem.FileExists(projectFileName))
            {
                // file exists - continue loading
                return true;
            }

            if (!this.settings.SecuredDrives.OfType<string>().Any(
                drive => projectFileName.StartsWith(drive, StringComparison.InvariantCultureIgnoreCase)))
            {
                // file does not exist
                // file is not known as saved on secure drive
                // continue loading because auto-save file may exist
                return true;
            }

            MessageBoxResult result = this.dialogs.ShowMessageBox(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    Resources.Question_StartSecureDriverX,
                    projectFileName),
                Resources.Header_LoadProject,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);
            if (result != MessageBoxResult.Yes)
            {
                // user does not want to start secure drive app
                // abort
                return false;
            }

            var starter = new SecureDriveStarter(this.fileSystem, this.processApi, projectFileName);
            if (!await starter.StartApplicationAsync())
            {
                // failed to start application
                // abort
                return false;
            }

            // app started
            // continue loading
            return true;
        }

        private bool LoadFile(string projectFileName, out bool autoSaveFileLoaded)
        {
            var result = MessageBoxResult.No;
            var autoSaveFileName = Defines.GetAutoSaveFileName(projectFileName);
            if (this.fileSystem.FileExists(autoSaveFileName))
            {
                result = this.dialogs.ShowMessageBox(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        Resources.Question_LoadAutoSaveProjectFileX,
                        projectFileName),
                    Resources.Header_LoadProject,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    this.fileSystem.FileDelete(autoSaveFileName);
                }
            }

            autoSaveFileLoaded = result == MessageBoxResult.Yes;

            string fileName = autoSaveFileLoaded ? autoSaveFileName : projectFileName;
            if (!this.fileSystem.FileExists(fileName))
            {
                return false;
            }

            var projectXml = this.fileSystem.ReadAllTextFromFile(fileName);
            this.ProjectData = AccountingData.Deserialize(projectXml);
            return true;
        }

        private void UpdateSettings(string projectFileName)
        {
            this.settings.RecentProject = projectFileName;

            var info = this.fileSystem.GetDrives().SingleOrDefault(
                x => projectFileName.StartsWith(x.RootPath, StringComparison.InvariantCultureIgnoreCase));
            string format = info.GetFormat?.Invoke() ?? string.Empty;
            if (format.Contains("cryptomator", StringComparison.InvariantCultureIgnoreCase)
                && !this.settings.SecuredDrives.Contains(info.RootPath))
            {
                this.settings.SecuredDrives.Add(info.RootPath);
            }

            this.settings.RecentProjects.Remove(projectFileName);
            this.settings.RecentProjects.Insert(0, projectFileName);
            while (this.settings.RecentProjects.Count > MaxRecentProjects)
            {
                this.settings.RecentProjects.RemoveAt(MaxRecentProjects);
            }
        }
    }
}
