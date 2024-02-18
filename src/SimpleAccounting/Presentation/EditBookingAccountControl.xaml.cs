// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

#nullable disable

namespace lg2de.SimpleAccounting.Presentation;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using lg2de.SimpleAccounting.Infrastructure;
using lg2de.SimpleAccounting.Model;

/// <summary>
///     Implements the control to edit single booking account.
/// </summary>
[SuppressMessage(
    "Major Code Smell", "S4004:Collection properties should be readonly",
    Justification = "FP for CustomControl")]
public partial class EditBookingAccountControl : INotifyPropertyChanged
{
    public static readonly DependencyProperty AccountIndexProperty
        = DependencyProperty.Register(
            "AccountIndex",
            typeof(int),
            typeof(EditBookingAccountControl),
            new PropertyMetadata(0));

    public static readonly DependencyProperty AccountNumberProperty
        = DependencyProperty.Register(
            "AccountNumber",
            typeof(ulong),
            typeof(EditBookingAccountControl),
            new PropertyMetadata((ulong)0));

    public static readonly DependencyProperty AccountsProperty
        = DependencyProperty.Register(
            "Accounts",
            typeof(List<AccountDefinition>),
            typeof(EditBookingAccountControl),
            new PropertyMetadata());

    public static readonly DependencyProperty AllowSplittingProperty
        = DependencyProperty.Register(
            "AllowSplitting",
            typeof(bool),
            typeof(EditBookingAccountControl),
            new PropertyMetadata(false, OnAllowSplittingChanged));

    public static readonly DependencyProperty SplitEntriesProperty
        = DependencyProperty.Register(
            "SplitEntries",
            typeof(ObservableCollection<SplitBookingViewModel>),
            typeof(EditBookingAccountControl),
            new PropertyMetadata(OnSplitEntriesChanged));

    public static readonly DependencyProperty BookingTextProperty
        = DependencyProperty.Register(
            "BookingText",
            typeof(string),
            typeof(EditBookingAccountControl),
            new PropertyMetadata());

    public static readonly DependencyProperty BookingValueProperty
        = DependencyProperty.Register(
            "BookingValue",
            typeof(double),
            typeof(EditBookingAccountControl),
            new PropertyMetadata(0.0));

    [ExcludeFromCodeCoverage(Justification = "The view class will not be tested.")]
    public EditBookingAccountControl()
    {
        this.InitializeComponent();
    }

    internal EditBookingAccountControl(bool _)
    {
    }

    /// <summary>
    ///     Gets or sets the account index (within list of accounts) for single booking mode.
    /// </summary>
    public int AccountIndex
    {
        get => (int)this.GetValue(AccountIndexProperty);
        set => this.SetValue(AccountIndexProperty, value);
    }

    /// <summary>
    ///     Gets or sets the account number for single booking mode.
    /// </summary>
    public ulong AccountNumber
    {
        get => (ulong)this.GetValue(AccountNumberProperty);
        set => this.SetValue(AccountNumberProperty, value);
    }

    /// <summary>
    ///     Gets or sets the list accounts.
    /// </summary>
    /// <remarks>
    ///     This list is used in single and in split booking mode.
    /// </remarks>
    public IEnumerable<AccountDefinition> Accounts
    {
        get => (List<AccountDefinition>)this.GetValue(AccountsProperty);
        set => this.SetValue(AccountsProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether split booking is allowed.
    /// </summary>
    public bool AllowSplitting
    {
        get => (bool)this.GetValue(AllowSplittingProperty);
        set => this.SetValue(AllowSplittingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the list of split bookings.
    /// </summary>
    public ObservableCollection<SplitBookingViewModel> SplitEntries
    {
        get => (ObservableCollection<SplitBookingViewModel>)this.GetValue(SplitEntriesProperty);
        set => this.SetValue(SplitEntriesProperty, value);
    }

    /// <summary>
    ///     Gets or sets the booking text for initial split booking entry.
    /// </summary>
    /// <remarks>
    ///     This property is used only for transition to split booking mode.
    /// </remarks>
    public string BookingText
    {
        get => (string)this.GetValue(BookingTextProperty);
        set => this.SetValue(BookingTextProperty, value);
    }

    /// <summary>
    ///     Gets or sets the booking value for initial split booking entry.
    /// </summary>
    /// <remarks>
    ///     This property is used only for transition to split booking mode.
    /// </remarks>
    public double BookingValue
    {
        get => (double)this.GetValue(BookingValueProperty);
        set => this.SetValue(BookingValueProperty, value);
    }

    /// <summary>
    ///     Gets the visibility of the split button according to the supported split mode.
    /// </summary>
    /// <remarks>
    ///     This property is used only in the control itself.
    /// </remarks>
    public Visibility SplitButtonVisibility => this.AllowSplitting ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    ///     Gets the grid span for the account combo box according to the supported split mode.
    /// </summary>
    /// <remarks>
    ///     This property is used only in the control itself.
    /// </remarks>
    public int AccountSelectionSpan
    {
        get
        {
            const int oneColumn = 1;
            const int twoColumns = 2;
            return this.AllowSplitting ? oneColumn : twoColumns;
        }
    }

    /// <summary>
    ///     Gets or sets the visibility for the single row mode controls.
    /// </summary>
    /// <remarks>
    ///     This property is used only in the control itself.
    /// </remarks>
    public Visibility SingleRowVisibility =>
        this.SplitEntries?.Any() == true ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    ///     Gets or sets the visibility for the split booking (multiple rows) mode controls.
    /// </summary>
    /// <remarks>
    ///     This property is used only in the control itself.
    /// </remarks>
    public Visibility SplitRowsVisibility => this.AllowSplitting && this.SplitEntries?.Any() == true
        ? Visibility.Visible
        : Visibility.Collapsed;

    /// <summary>
    ///     Gets the command to start splitting.
    /// </summary>
    /// <remarks>
    ///     This property is used only in the control itself.
    /// </remarks>
    public ICommand SplitCommand => new AsyncCommand(
        () =>
        {
            this.SplitEntries.Add(
                new SplitBookingViewModel
                {
                    AccountNumber = this.AccountNumber,
                    BookingText = this.BookingText,
                    BookingValue = this.BookingValue
                });
        },
        () => this.AllowSplitting);

    public ICommand AddSplitEntryCommand => new AsyncCommand(
        parameter =>
        {
            if (parameter is not SplitBookingViewModel currentViewModel)
            {
                return Task.CompletedTask;
            }

            var index = this.SplitEntries.IndexOf(currentViewModel);
            this.SplitEntries.Insert(index + 1, new SplitBookingViewModel());
            return Task.CompletedTask;
        },
        () => this.AllowSplitting);

    public ICommand RemoveSplitEntryCommand => new AsyncCommand(
        parameter =>
        {
            if (parameter is not SplitBookingViewModel viewModel)
            {
                return Task.CompletedTask;
            }

            this.SplitEntries.Remove(viewModel);
            return Task.CompletedTask;
        },
        () => this.AllowSplitting);

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static void OnAllowSplittingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EditBookingAccountControl)d;
        control.OnPropertyChanged(nameof(control.SplitButtonVisibility));
        control.OnPropertyChanged(nameof(control.AccountSelectionSpan));
        control.OnPropertyChanged(nameof(control.SingleRowVisibility));
        control.OnPropertyChanged(nameof(control.SplitRowsVisibility));
    }

    private static void OnSplitEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EditBookingAccountControl)d;

        var collection = (ObservableCollection<SplitBookingViewModel>)e.OldValue;
        if (collection != null)
        {
            collection.CollectionChanged -= control.OnSplitEntriesCollectionChanged;
        }

        collection = (ObservableCollection<SplitBookingViewModel>)e.NewValue;
        if (collection != null)
        {
            collection.CollectionChanged += control.OnSplitEntriesCollectionChanged;
        }

        control.OnPropertyChanged(nameof(control.SingleRowVisibility));
        control.OnPropertyChanged(nameof(control.SplitRowsVisibility));
    }

    private void OnSplitEntriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        this.OnPropertyChanged(nameof(this.SingleRowVisibility));
        this.OnPropertyChanged(nameof(this.SplitRowsVisibility));
    }
}
