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
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Infrastructure;
    using lg2de.SimpleAccounting.Model;
    using lg2de.SimpleAccounting.Properties;

    /// <summary>
    ///     Implements the view model to show all accounts.
    /// </summary>
    internal class AccountsViewModel : Screen, IAccountsViewModel
    {
        private readonly IList<AccountViewModel> allAccounts = new List<AccountViewModel>();
        private readonly IProjectData projectData;
        private readonly IWindowManager windowManager;

        private AccountViewModel? selectedAccount;

        public AccountsViewModel(IWindowManager windowManager, IProjectData projectData)
        {
            this.windowManager = windowManager;
            this.projectData = projectData;
        }

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
            get => this.projectData.ShowInactiveAccounts;
            set
            {
                this.projectData.ShowInactiveAccounts = value;
                this.RefreshAccountList();
            }
        }

        public void OnDataLoaded()
        {
            this.LoadAccounts(this.projectData.Storage.Accounts);
        }

        public void LoadAccounts(IReadOnlyCollection<AccountingDataAccountGroup> accounts)
        {
            this.allAccounts.Clear();
            foreach (var accountGroup in accounts)
            {
                foreach (AccountDefinition account in accountGroup.Account)
                {
                    var accountModel = CreateViewModel(account, accountGroup);
                    this.allAccounts.Add(accountModel);
                }
            }

            this.RefreshAccountList();

            AccountViewModel CreateViewModel(
                AccountDefinition account, AccountingDataAccountGroup accountGroup)
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

                if (account.ImportMapping == null)
                {
                    return accountModel;
                }

                accountModel.IsImportActive = true;
                var dateColumn = account.ImportMapping.Columns.FirstOrDefault(
                    x => x.Target == AccountDefinitionImportMappingColumnTarget.Date);
                accountModel.ImportDateSource = dateColumn?.Source;
                accountModel.ImportDateIgnorePattern = dateColumn?.IgnorePattern;
                var nameColumn = account.ImportMapping.Columns.FirstOrDefault(
                    x => x.Target == AccountDefinitionImportMappingColumnTarget.Name);
                accountModel.ImportNameSource = nameColumn?.Source;
                accountModel.ImportNameIgnorePattern = nameColumn?.IgnorePattern;
                var textColumn = account.ImportMapping.Columns.FirstOrDefault(
                    x => x.Target == AccountDefinitionImportMappingColumnTarget.Text);
                accountModel.ImportTextSource = textColumn?.Source;
                accountModel.ImportTextIgnorePattern = textColumn?.IgnorePattern;
                var valueColumn = account.ImportMapping.Columns.FirstOrDefault(
                    x => x.Target == AccountDefinitionImportMappingColumnTarget.Value);
                accountModel.ImportValueSource = valueColumn?.Source;
                accountModel.ImportValueIgnorePattern = valueColumn?.IgnorePattern;

                if (account.ImportMapping.Patterns == null)
                {
                    return accountModel;
                }

                accountModel.ImportPatterns = new ObservableCollection<ImportPatternViewModel>(
                    account.ImportMapping.Patterns.Select(
                        x => new ImportPatternViewModel
                        {
                            Expression = x.Expression,
                            AccountId = x.AccountID,
                            Value = x.ValueSpecified ? x.Value.ToViewModel() : (double?)null
                        }));

                return accountModel;
            }
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
            // setup new view model for the new account
            var accountGroup = this.projectData.Storage.Accounts.First();
            var accountVm = new AccountViewModel
            {
                DisplayName = Resources.Header_CreateAccount,
                Group = accountGroup,
                Groups = this.projectData.Storage.Accounts,
                IsValidIdentifierFunc = id => this.allAccounts.All(a => a.Identifier != id)
            };
            this.UpdateImportCandidateAccounts(accountVm);

            // get user input
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
            accountGroup.Account.Add(newAccount);
            accountGroup.Account = accountVm.Group.Account.OrderBy(x => x.ID).ToList();

            // update view model
            this.allAccounts.Add(accountVm);
            this.RefreshAccountList();

            // raise modification
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

        private void ShowEditAccountDialog(AccountViewModel selectedAccountViewModel)
        {
            // clone view model for easy rollback
            var clonedViewModel = selectedAccountViewModel.Clone();
            clonedViewModel.DisplayName = Resources.Header_EditAccount;
            var invalidIds = this.allAccounts.Select(x => x.Identifier)
                .Where(x => x != selectedAccountViewModel.Identifier).ToList();
            clonedViewModel.IsValidIdentifierFunc = id => !invalidIds.Contains(id);
            this.UpdateImportCandidateAccounts(clonedViewModel);

            // get user input
            var result = this.windowManager.ShowDialog(clonedViewModel);
            if (result != true)
            {
                return;
            }

            // update database
            var accountData =
                this.projectData.Storage.AllAccounts.Single(x => x.ID == selectedAccountViewModel.Identifier);
            accountData.Name = clonedViewModel.Name;
            accountData.Type = clonedViewModel.Type;
            accountData.Active = clonedViewModel.IsActivated;
            if (selectedAccountViewModel.Identifier != clonedViewModel.Identifier)
            {
                // account identifier has been changed
                // update all references of this identifier
                accountData.ID = clonedViewModel.Identifier;

                selectedAccountViewModel.Group!.Account =
                    selectedAccountViewModel.Group.Account.OrderBy(x => x.ID).ToList();

                this.projectData.Storage.Journal.ForEach(
                    j => j.Booking?.ForEach(
                        b =>
                        {
                            b.Credit.ForEach(
                                c => UpdateAccount(c, selectedAccountViewModel.Identifier, clonedViewModel.Identifier));
                            b.Debit.ForEach(
                                d => UpdateAccount(d, selectedAccountViewModel.Identifier, clonedViewModel.Identifier));
                        }));
            }

            this.SaveImportConfiguration(clonedViewModel, accountData);

            // update view model
            selectedAccountViewModel.Name = clonedViewModel.Name;
            selectedAccountViewModel.Group = clonedViewModel.Group;
            selectedAccountViewModel.Type = clonedViewModel.Type;
            selectedAccountViewModel.Identifier = clonedViewModel.Identifier;
            selectedAccountViewModel.IsActivated = clonedViewModel.IsActivated;
            this.RefreshAccountList();
            selectedAccountViewModel.Refresh();
            this.projectData.TriggerJournalChanged();

            // raise modification
            this.projectData.IsModified = true;

            static void UpdateAccount(BookingValue entry, ulong oldIdentifier, ulong newIdentifier)
            {
                if (entry.Account == oldIdentifier)
                {
                    entry.Account = newIdentifier;
                }
            }
        }

        private void UpdateImportCandidateAccounts(AccountViewModel accountViewModel)
        {
            accountViewModel.ImportRemoteAccounts =
                this.projectData.Storage.Accounts.SelectMany(x => x.Account)
                    .Where(x => x.Type != AccountDefinitionType.Carryforward)
                    .Where(x => x.ID != accountViewModel.Identifier).ToList();
            foreach (var importPattern in accountViewModel.ImportPatterns)
            {
                importPattern.Account =
                    accountViewModel.ImportRemoteAccounts.FirstOrDefault(x => x.ID == importPattern.AccountId);
            }
        }

        private void SaveImportConfiguration(AccountViewModel viewModel, AccountDefinition accountDefinition)
        {
            if (!viewModel.IsImportActive)
            {
                accountDefinition.ImportMapping = null;
                return;
            }

            // save required items
            accountDefinition.ImportMapping = new AccountDefinitionImportMapping
            {
                Columns = new List<AccountDefinitionImportMappingColumn>
                {
                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Date,
                        Source = viewModel.ImportDateSource,
                        IgnorePattern = viewModel.ImportDateIgnorePattern
                    },
                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Value,
                        Source = viewModel.ImportValueSource,
                        IgnorePattern = viewModel.ImportValueIgnorePattern
                    }
                }
            };

            // save optional items
            if (!string.IsNullOrWhiteSpace(viewModel.ImportTextSource))
            {
                accountDefinition.ImportMapping.Columns.Add(
                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Text,
                        Source = viewModel.ImportTextSource,
                        IgnorePattern = viewModel.ImportTextIgnorePattern
                    });
            }

            if (!string.IsNullOrWhiteSpace(viewModel.ImportNameSource))
            {
                accountDefinition.ImportMapping.Columns.Add(
                    new AccountDefinitionImportMappingColumn
                    {
                        Target = AccountDefinitionImportMappingColumnTarget.Name,
                        Source = viewModel.ImportNameSource,
                        IgnorePattern = viewModel.ImportNameIgnorePattern
                    });
            }

            if (!viewModel.ImportPatterns.Any())
            {
                return;
            }

            accountDefinition.ImportMapping.Patterns = viewModel.ImportPatterns.Select(
                x => new AccountDefinitionImportMappingPattern
                {
                    Expression = x.Expression,
                    Value = x.Value?.ToModelValue() ?? 0,
                    ValueSpecified = x.Value.HasValue,
                    AccountID = x.Account?.ID ?? 0
                }).ToList();
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
