// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;
    using lg2de.SimpleAccounting.Reports;
    using Screen = Caliburn.Micro.Screen;

    /// <summary>
    ///     Implements the view model for the complete menu including all application commands.
    /// </summary>
    internal class MenuViewModel : Screen, IMenuViewModel
    {
        private readonly IBusy busy;
        private readonly IDialogs dialogs;
        private readonly IProcess processApi;
        private readonly IProjectData projectData;
        private readonly IReportFactory reportFactory;

        public MenuViewModel(
            IProjectData projectData,
            IBusy busy, IReportFactory reportFactory,
            IProcess processApi, IDialogs dialogs)
        {
            this.busy = busy;
            this.projectData = projectData;
            this.processApi = processApi;
            this.dialogs = dialogs;
            this.reportFactory = reportFactory;
        }

        public ICommand NewProjectCommand => new RelayCommand(
            _ =>
            {
                if (!this.projectData.CanDiscardModifiedProject())
                {
                    return;
                }

                this.projectData.NewProject();
            });

        public ICommand OpenProjectCommand => new RelayCommand(
            _ => this.OnOpenProject());

        public ICommand SaveProjectCommand => new RelayCommand(
            _ => this.projectData.SaveProject(),
            _ => this.projectData.IsModified);

        public ICommand ProjectOptionsCommand => new RelayCommand(
            _ => this.projectData.EditProjectOptions());

        public ICommand SwitchCultureCommand => new RelayCommand(
            cultureName =>
            {
                this.projectData.Settings.Culture = cultureName.ToString();
                this.NotifyOfPropertyChange(nameof(this.IsGermanCulture));
                this.NotifyOfPropertyChange(nameof(this.IsEnglishCulture));
                this.NotifyOfPropertyChange(nameof(this.IsFrenchCulture));
                this.NotifyOfPropertyChange(nameof(this.IsSystemCulture));
                this.dialogs.ShowMessageBox(
                    Resources.Information_CultureChangeRestartRequired,
                    Resources.Header_SettingsChanged,
                    icon: MessageBoxImage.Information);
            });

        public bool IsGermanCulture => this.projectData.Settings.Culture == "de";
        public bool IsEnglishCulture => this.projectData.Settings.Culture == "en";
        public bool IsFrenchCulture => this.projectData.Settings.Culture == "fr";
        public bool IsSystemCulture => this.projectData.Settings.Culture == string.Empty;

        public ObservableCollection<MenuItemViewModel> RecentProjects { get; }
            = new ObservableCollection<MenuItemViewModel>();

        public ICommand AddBookingsCommand => new RelayCommand(
            _ => this.projectData.ShowAddBookingDialog(this.projectData.ShowInactiveAccounts),
            _ => !this.projectData.CurrentYear.Closed);

        public ICommand DuplicateBookingsCommand => new RelayCommand(
            this.OnDuplicateBooking,
            _ => !this.projectData.CurrentYear.Closed);

        public ICommand EditBookingCommand => new RelayCommand(
            this.OnEditBooking,
            _ => !this.projectData.CurrentYear.Closed);

        public ICommand ImportBookingsCommand => new RelayCommand(
            _ => this.projectData.ShowImportDialog(),
            _ => !this.projectData.CurrentYear.Closed);

        public ICommand CloseYearCommand => new RelayCommand(
            this.OnCloseYear,
            _ => !this.projectData.CurrentYear.Closed);

        public ICommand TotalJournalReportCommand => new RelayCommand(
            _ => this.OnTotalJournalReport(),
            _ => this.projectData.CurrentYear.Booking.Any());

        public ICommand AccountJournalReportCommand => new RelayCommand(
            _ => this.OnAccountJournalReport(),
            _ => this.projectData.CurrentYear.Booking.Any());

        public ICommand TotalsAndBalancesReportCommand => new RelayCommand(
            _ => this.OnTotalsAndBalancesReport(),
            _ => this.projectData.CurrentYear.Booking.Any());

        public ICommand AssetBalancesReportCommand => new RelayCommand(
            _ => this.OnAssetBalancesReport(),
            _ => this.projectData.CurrentYear.Booking.Any());

        public ICommand AnnualBalanceReportCommand => new RelayCommand(
            _ => this.OnAnnualBalanceReport(),
            _ => this.projectData.CurrentYear.Booking.Any());

        public ObservableCollection<MenuItemViewModel> BookingYears { get; }
            = new ObservableCollection<MenuItemViewModel>();

        public ICommand HelpAboutCommand => new RelayCommand(
            _ => this.processApi.ShellExecute(Defines.ProjectUrl));

        public ICommand HelpFeedbackCommand => new RelayCommand(
            _ => this.processApi.ShellExecute(Defines.NewIssueUrl));

        public void BuildRecentProjectsMenu()
        {
            if (this.projectData.Settings.RecentProjects == null)
            {
                return;
            }

            foreach (var project in this.projectData.Settings.RecentProjects)
            {
                var command = new AsyncCommand(this.busy, () => this.OnLoadRecentProjectAsync(project));
                this.RecentProjects.Add(new MenuItemViewModel(project, command));
            }
        }

        public void OnDataLoaded()
        {
            // build the list of booking years from loaded data
            this.UpdateBookingYears();

            // select last booking year after loading
            this.BookingYears.LastOrDefault()?.Command.Execute(null);
        }

        private async Task OnLoadRecentProjectAsync(string project)
        {
            var loadResult = await this.projectData.LoadFromFileAsync(project);
            if (loadResult != OperationResult.Failed)
            {
                return;
            }

            // failed to load, remove from menu
            // keep in menu if aborted (e.g. SecureDrive not available)
            var item = this.RecentProjects.FirstOrDefault(x => x.Header == project);
            this.RecentProjects.Remove(item);
            this.projectData.Settings.RecentProjects.Remove(project);
        }

        private void OnOpenProject()
        {
            (DialogResult result, var fileName) =
                this.dialogs.ShowOpenFileDialog(filter: Resources.FileFilter_MainProject);
            if (result != DialogResult.OK)
            {
                return;
            }

            this.busy.IsBusy = true;
            Task.Run(
                async () =>
                {
                    await this.projectData.LoadFromFileAsync(fileName);
                    await Execute.OnUIThreadAsync(() => this.busy.IsBusy = false);
                });
        }

        private void UpdateBookingYears()
        {
            this.BookingYears.Clear();
            foreach (var year in this.projectData.Storage.Journal.Select(x => x.Year))
            {
                var menu = new MenuItemViewModel(
                    year.ToString(CultureInfo.InvariantCulture),
                    new AsyncCommand(this.busy, () => this.projectData.SelectYear(year)));
                this.BookingYears.Add(menu);
            }
        }

        private void OnEditBooking(object commandParameter)
        {
            if (!(commandParameter is IJournalItem journalItem))
            {
                return;
            }

            this.projectData.ShowEditBookingDialog(journalItem, this.projectData.ShowInactiveAccounts);
        }

        private void OnDuplicateBooking(object commandParameter)
        {
            if (!(commandParameter is IJournalItem journalItem))
            {
                return;
            }

            this.projectData.ShowDuplicateBookingDialog(journalItem, this.projectData.ShowInactiveAccounts);
        }

        private void OnCloseYear(object _)
        {
            if (!this.projectData.CloseYear())
            {
                return;
            }

            // refresh menu and select the new year
            this.UpdateBookingYears();
            this.BookingYears.Last().Command.Execute(null);
        }

        private void OnTotalJournalReport()
        {
            var report = this.reportFactory.CreateTotalJournal(this.projectData);
            report.CreateReport(Resources.Header_Journal);
            report.ShowPreview(Resources.Header_Journal);
        }

        private void OnAccountJournalReport()
        {
            var report = this.reportFactory.CreateAccountJournal(this.projectData);
            report.PageBreakBetweenAccounts =
                this.projectData.Storage.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
            report.CreateReport(Resources.Header_AccountSheets);
            report.ShowPreview(Resources.Header_AccountSheets);
        }

        private void OnTotalsAndBalancesReport()
        {
            var report = this.reportFactory.CreateTotalsAndBalances(
                this.projectData, this.projectData.Storage.Accounts);
            report.CreateReport(Resources.Header_TotalsAndBalances);
            report.ShowPreview(Resources.Header_TotalsAndBalances);
        }

        private void OnAssetBalancesReport()
        {
            var accountGroups = new List<AccountingDataAccountGroup>();
            foreach (var group in this.projectData.Storage.Accounts)
            {
                var assertAccounts = group.Account
                    .Where(a => a.Type == AccountDefinitionType.Asset).ToList();
                if (assertAccounts.Count <= 0)
                {
                    // ignore group
                    continue;
                }

                accountGroups.Add(new AccountingDataAccountGroup { Name = group.Name, Account = assertAccounts });
            }

            var report = this.reportFactory.CreateTotalsAndBalances(this.projectData, accountGroups);
            this.projectData.Storage.Setup?.Reports?.TotalsAndBalancesReport?.ForEach(report.Signatures.Add);
            report.CreateReport(Resources.Header_AssetBalances);
            report.ShowPreview(Resources.Header_AssetBalances);
        }

        private void OnAnnualBalanceReport()
        {
            var report = this.reportFactory.CreateAnnualBalance(this.projectData);
            string title = Resources.Header_AnnualBalance;
            report.CreateReport(title);
            report.ShowPreview(title);
        }
    }
}
