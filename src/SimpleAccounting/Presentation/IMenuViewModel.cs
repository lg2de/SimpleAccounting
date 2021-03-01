// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;

    /// <summary>
    ///     Defines abstraction for <see cref="MenuViewModel"/>.
    /// </summary>
    internal interface IMenuViewModel : INotifyPropertyChanged
    {
        ICommand NewProjectCommand { get; }
        ICommand OpenProjectCommand { get; }
        ICommand SaveProjectCommand { get; }

        ObservableCollection<MenuItemViewModel> RecentProjects { get; }

        ICommand SwitchCultureCommand { get; }
        bool IsGermanCulture { get; }
        bool IsEnglishCulture { get; }
        bool IsSystemCulture { get; }

        ICommand AddBookingsCommand { get; }
        ICommand EditBookingCommand { get; }
        ICommand ImportBookingsCommand { get; }
        ICommand CloseYearCommand { get; }
        ObservableCollection<MenuItemViewModel> BookingYears { get; }

        ICommand TotalJournalReportCommand { get; }
        ICommand AccountJournalReportCommand { get; }
        ICommand TotalsAndBalancesReportCommand { get; }
        ICommand AssetBalancesReportCommand { get; }
        ICommand AnnualBalanceReportCommand { get; }

        ICommand HelpAboutCommand { get; }
        ICommand HelpFeedbackCommand { get; }

        void BuildRecentProjectsMenu();
        void OnDataLoaded();
    }
}
