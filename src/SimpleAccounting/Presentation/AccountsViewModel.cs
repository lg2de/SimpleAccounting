// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    internal class AccountsViewModel : Screen
    {
        private readonly IList<AccountViewModel> allAccounts = new List<AccountViewModel>();
        private readonly IProjectData projectData;
        private readonly IWindowManager windowManager;

        private AccountViewModel? selectedAccount;
        private bool showInactiveAccounts;

        public AccountsViewModel(IWindowManager windowManager, IProjectData projectData)
        {
            this.windowManager = windowManager;
            this.projectData = projectData;

            this.projectData.DataLoaded += (_, __) =>
            {
                this.LoadAccounts(this.projectData.Storage.Accounts);
            };
        }

        public IEnumerable<AccountViewModel> AllAccounts => this.allAccounts;

        public ObservableCollection<AccountViewModel> AccountList { get; }
            = new ObservableCollection<AccountViewModel>();

        public AccountViewModel? SelectedAccount
        {
            get => this.selectedAccount;
            set
            {
                this.selectedAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ICommand AccountSelectionCommand => new RelayCommand(
            o =>
            {
                if (o is AccountViewModel account)
                {
                    this.SelectedAccount = account;
                }
            });

        public bool ShowInactiveAccounts
        {
            get => this.showInactiveAccounts;
            set
            {
                this.showInactiveAccounts = value;
                this.RefreshAccountList();
            }
        }

        public void LoadAccounts(IReadOnlyCollection<AccountingDataAccountGroup> accounts)
        {
            this.allAccounts.Clear();
            foreach (var accountGroup in accounts)
            {
                foreach (var account in accountGroup.Account)
                {
                    var accountModel = new AccountViewModel
                    {
                        Identifier = account.ID,
                        Name = account.Name,
                        Group = accountGroup,
                        Groups = accounts,
                        Type = account.Type,
                        IsActivated = account.Active
                    };
                    this.allAccounts.Add(accountModel);
                }
            }

            this.RefreshAccountList();
        }

        public void SelectFirstAccount()
        {
            var firstBooking = this.projectData.CurrentYear.Booking?.FirstOrDefault();
            if (firstBooking != null)
            {
                var firstAccount = firstBooking
                    .Credit.Select(x => x.Account)
                    .Concat(firstBooking.Debit.Select(x => x.Account))
                    .Min();
                this.SelectedAccount = this.AccountList.Single(x => x.Identifier == firstAccount);
            }
            else
            {
                this.SelectedAccount = this.AccountList.FirstOrDefault();
            }
        }
        
        public void ShowNewAccountDialog()
        {
            var accountVm = new AccountViewModel
            {
                DisplayName = Resources.Header_CreateAccount,
                Group = this.projectData.Storage.Accounts.First(),
                Groups = this.projectData.Storage.Accounts,
                IsValidIdentifierFunc = id => this.AllAccounts.All(a => a.Identifier != id)
            };
            var result = this.windowManager.ShowDialog(accountVm);
            if (result != true)
            {
                return;
            }

            // update database
            var newAccount = new AccountDefinition
            {
                ID = accountVm.Identifier,
                Name = accountVm.Name,
                Type = accountVm.Type,
                Active = accountVm.IsActivated
            };
            accountVm.Group.Account.Add(newAccount);
            accountVm.Group.Account = accountVm.Group.Account.OrderBy(x => x.ID).ToList();

            // update view
            this.allAccounts.Add(accountVm);
            this.RefreshAccountList();

            this.projectData.IsModified = true;
        }

        public void OnEditAccount(object commandParameter)
        {
            if (!(commandParameter is AccountViewModel account))
            {
                return;
            }

            this.ShowEditAccountDialog(account);
        }

        private void ShowEditAccountDialog(AccountViewModel account)
        {
            var vm = account.Clone();
            vm.DisplayName = Resources.Header_EditAccount;
            var invalidIds = this.AllAccounts.Select(x => x.Identifier).Where(x => x != account.Identifier)
                .ToList();
            vm.IsValidIdentifierFunc = id => !invalidIds.Contains(id);
            var result = this.windowManager.ShowDialog(vm);
            if (result != true)
            {
                return;
            }

            // update database
            var accountData = this.projectData.Storage.AllAccounts.Single(x => x.ID == account.Identifier);
            accountData.Name = vm.Name;
            accountData.Type = vm.Type;
            accountData.Active = vm.IsActivated;
            if (account.Identifier != vm.Identifier)
            {
                accountData.ID = vm.Identifier;
                account.Group!.Account = account.Group.Account.OrderBy(x => x.ID).ToList();

                this.projectData.Storage.Journal.ForEach(
                    j => j.Booking?.ForEach(
                        b =>
                        {
                            b.Credit.ForEach(c => UpdateAccount(c, account.Identifier, vm.Identifier));
                            b.Debit.ForEach(d => UpdateAccount(d, account.Identifier, vm.Identifier));
                        }));
            }

            // update view
            account.Name = vm.Name;
            account.Group = vm.Group;
            account.Type = vm.Type;
            account.Identifier = vm.Identifier;
            account.IsActivated = vm.IsActivated;
            this.RefreshAccountList();
            account.Refresh();
            this.projectData.TriggerJournalChanged();
            this.projectData.IsModified = true;

            static void UpdateAccount(BookingValue entry, ulong oldIdentifier, ulong newIdentifier)
            {
                if (entry.Account == oldIdentifier)
                {
                    entry.Account = newIdentifier;
                }
            }
        }

        private void RefreshAccountList()
        {
            IEnumerable<AccountViewModel> accounts = this.allAccounts;
            if (!this.ShowInactiveAccounts)
            {
                accounts = accounts.Where(x => x.IsActivated);
            }

            var sorted = accounts.OrderBy(x => x.Identifier).ToList();

            this.AccountList.Clear();
            sorted.ForEach(this.AccountList.Add);
        }
    }
}
