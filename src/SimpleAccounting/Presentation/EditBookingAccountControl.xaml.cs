// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Windows;
    using lg2de.SimpleAccounting.Model;

    /// <summary>
    ///     Implements the control to edit single booking account.
    /// </summary>
    public partial class EditBookingAccountControl
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
                new PropertyMetadata(false));

        public EditBookingAccountControl()
        {
            this.InitializeComponent();
        }

        public int AccountIndex
        {
            get => (int)this.GetValue(AccountIndexProperty);
            set => this.SetValue(AccountIndexProperty, value);
        }

        public ulong AccountNumber
        {
            get => (ulong)this.GetValue(AccountNumberProperty);
            set => this.SetValue(AccountNumberProperty, value);
        }

        public IEnumerable<AccountDefinition> Accounts
        {
            get => (List<AccountDefinition>)this.GetValue(AccountsProperty);
            set => this.SetValue(AccountsProperty, value);
        }

        public bool AllowSplitting
        {
            get => (bool)this.GetValue(AllowSplittingProperty);
            set => this.SetValue(AllowSplittingProperty, value);
        }

        public Visibility SplitButtonVisibility => this.AllowSplitting ? Visibility.Visible : Visibility.Collapsed;

        public int AccountSelectionSpan => this.AllowSplitting ? 1 : 2;
    }
}
