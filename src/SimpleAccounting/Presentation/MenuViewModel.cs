// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
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
    private readonly IProjectData projectData;
    private readonly IBusy busy;
    private readonly IReportFactory reportFactory;
    private readonly IClock clock;
    private readonly IProcess processApi;
    private readonly IDialogs dialogs;

    public MenuViewModel(
        IProjectData projectData,
        IBusy busy, IReportFactory reportFactory,
        IClock clock,
        IProcess processApi, IDialogs dialogs)
    {
        this.projectData = projectData;
        this.busy = busy;
        this.reportFactory = reportFactory;
        this.clock = clock;
        this.processApi = processApi;
        this.dialogs = dialogs;
    }

    public ICommand NewProjectCommand => new AsyncCommand(
        async () =>
        {
            if (!await this.projectData.TryCloseAsync())
            {
                // abort
                return;
            }

            this.projectData.NewProject();
        });

    public ICommand OpenProjectCommand => new AsyncCommand(
        this.OnOpenProjectAsync);

    public IAsyncCommand SaveProjectCommand => new AsyncCommand(
        this.OnSaveProjectAsync,
        () => this.projectData.IsModified);

    public ICommand ProjectOptionsCommand => new AsyncCommand(
        this.projectData.EditProjectOptionsAsync);

    public ICommand SwitchCultureCommand => new AsyncCommand(
        cultureName =>
        {
            ArgumentNullException.ThrowIfNull(cultureName);

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

    public ObservableCollection<MenuItemViewModel> RecentProjects { get; } = [];

    public ICommand AddBookingsCommand => new AsyncCommand(
        () => this.projectData.ShowAddBookingDialogAsync(this.clock.Now().Date, this.projectData.ShowInactiveAccounts),
        () => !this.projectData.CurrentYear.Closed);

    public ICommand DuplicateBookingsCommand => new AsyncCommand(
        this.OnDuplicateBookingAsync,
        () => !this.projectData.CurrentYear.Closed);

    public ICommand EditBookingCommand => new AsyncCommand(
        this.OnEditBookingAsync,
        () => !this.projectData.CurrentYear.Closed);

    public ICommand ImportBookingsCommand => new AsyncCommand(
        this.projectData.ShowImportDialogAsync,
        () => !this.projectData.CurrentYear.Closed);

    public ICommand CloseYearCommand => new AsyncCommand(
        this.OnCloseYearAsync,
        () => !this.projectData.CurrentYear.Closed);

    public ICommand TotalJournalReportCommand => new AsyncCommand(
        this.OnTotalJournalReport,
        () => this.projectData.CurrentYear.Booking.Count != 0);

    public ICommand AccountJournalReportCommand => new AsyncCommand(
        this.OnAccountJournalReport,
        () => this.projectData.CurrentYear.Booking.Count != 0);

    public ICommand TotalsAndBalancesReportCommand => new AsyncCommand(
        this.OnTotalsAndBalancesReport,
        () => this.projectData.CurrentYear.Booking.Count != 0);

    public ICommand AssetBalancesReportCommand => new AsyncCommand(
        this.OnAssetBalancesReport,
        () => this.projectData.CurrentYear.Booking.Count != 0);

    public ICommand AnnualBalanceReportCommand => new AsyncCommand(
        this.OnAnnualBalanceReport,
        () => this.projectData.CurrentYear.Booking.Count != 0);

    public ObservableCollection<MenuItemViewModel> BookingYears { get; } = [];

    public ICommand HelpAboutCommand => new AsyncCommand(
        () => this.processApi.ShellExecute(Defines.ProjectUrl));

    public ICommand HelpFeedbackCommand => new AsyncCommand(
        () => this.processApi.ShellExecute(Defines.NewBugUrl));

    public void BuildRecentProjectsMenu()
    {
        if (this.projectData.Settings.RecentProjects == null)
        {
            return;
        }

        this.RecentProjects.Clear();
        foreach (var project in this.projectData.Settings.RecentProjects.Cast<string>())
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
        if (loadResult == OperationResult.Failed)
        {
            // failed to load, remove from menu
            // keep in menu if aborted (e.g. SecureDrive not available)
            this.projectData.Settings.RecentProjects.Remove(project);
        }

        this.BuildRecentProjectsMenu();
    }

    private Task OnOpenProjectAsync()
    {
        (DialogResult result, var fileName) =
            this.dialogs.ShowOpenFileDialog(filter: Resources.FileFilter_MainProject);
        if (result != DialogResult.OK)
        {
            return Task.CompletedTask;
        }

        this.busy.IsBusy = true;
        return Task.Run(
            async () =>
            {
                await this.projectData.LoadFromFileAsync(fileName);
                await Execute.OnUIThreadAsync(
                    () =>
                    {
                        this.busy.IsBusy = false;
                        this.BuildRecentProjectsMenu();

                        return Task.CompletedTask;
                    });
            });
    }

    private async Task OnSaveProjectAsync()
    {
        await this.projectData.SaveProjectAsync();
        this.BuildRecentProjectsMenu();
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

    private Task OnEditBookingAsync(object? commandParameter)
    {
        if (commandParameter is not IJournalItem journalItem)
        {
            return Task.CompletedTask;
        }

        return this.projectData.ShowEditBookingDialogAsync(journalItem, this.projectData.ShowInactiveAccounts);
    }

    private Task OnDuplicateBookingAsync(object? commandParameter)
    {
        if (commandParameter is not IJournalItem journalItem)
        {
            return Task.CompletedTask;
        }

        return this.projectData.ShowDuplicateBookingDialogAsync(journalItem, this.projectData.ShowInactiveAccounts);
    }

    private async Task OnCloseYearAsync()
    {
        if (!await this.projectData.CloseYearAsync())
        {
            return;
        }

        // refresh menu and select the new year
        this.UpdateBookingYears();
        await this.BookingYears[^1].Command.ExecuteAsync(null);
    }

    private void OnTotalJournalReport()
    {
        var report = this.reportFactory.CreateTotalJournal(this.projectData);
        report.CreateReport();
        report.ShowPreview(Resources.Header_Journal);
    }

    private void OnAccountJournalReport()
    {
        var report = this.reportFactory.CreateAccountJournal(this.projectData);
        report.PageBreakBetweenAccounts =
            this.projectData.Storage.Setup?.Reports?.AccountJournalReport?.PageBreakBetweenAccounts ?? false;
        report.CreateReport();
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
        report.CreateReport();
        report.ShowPreview(title);
    }
}
