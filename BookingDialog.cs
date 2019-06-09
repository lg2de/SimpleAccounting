// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace lg2de.SimpleAccounting
{
    public partial class BookingDialog : Form
    {
        private readonly Regex accountExpression = new Regex("\\d+");
        private ShellViewModel theMainForm;

        public BookingDialog(ShellViewModel form)
        {
            InitializeComponent();

            theMainForm = form;

            this.SetNextBookIdent();
            var accountNames = this.theMainForm.GetAccounts().ToArray();
            this.comboBoxCreditAccount.Items.AddRange(accountNames);
            this.comboBoxDebitAccount.Items.AddRange(accountNames);
        }

        private void SetNextBookIdent()
        {
            var maxIdent = theMainForm.GetMaxBookIdent();
            maxIdent++;
            this.textBoxBookIdent.Text = maxIdent.ToString();
        }

        private void OnBookClick(object sender, EventArgs e)
        {
            theMainForm.SetBookDate(dateBookDate.Value);
            if (textBoxBookIdent.Text.Length == 0)
            {
                return;
            }

            theMainForm.SetBookIdent(Convert.ToUInt64(textBoxBookIdent.Text));
            if (textBoxDebitAccount.Text.Length == 0)
            {
                return;
            }

            theMainForm.AddDebitEntry(Convert.ToUInt64(textBoxDebitAccount.Text), Convert.ToInt32(textValue.Text), comboBookText.Text);
            if (textBoxCreditAccount.Text.Length == 0)
            {
                return;
            }

            theMainForm.AddCreditEntry(Convert.ToUInt64(textBoxCreditAccount.Text), Convert.ToInt32(textValue.Text), comboBookText.Text);
            theMainForm.RegisterBooking();

            this.SetNextBookIdent();
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnTextSelectionChangeCommitted(object sender, EventArgs e)
        {
            switch (this.comboBookText.SelectedIndex)
            {
                case 0:
                    // Fahrtkosten
                    this.textBoxDebitAccount.Text = "630";
                    break;

                case 1:
                    // Speisen
                    this.textBoxDebitAccount.Text = "610";
                    break;

                case 2:
                    // Webpräsenz
                    this.textBoxDebitAccount.Text = "620";
                    this.textBoxCreditAccount.Text = "11908";
                    break;

                case 3:
                    // AKD
                    this.textBoxDebitAccount.Text = "20001";
                    this.textBoxCreditAccount.Text = "400";
                    break;
            }
        }

        private void OnDebitAccountSelectionChangeCommitted(object sender, EventArgs e)
        {
            this.textBoxDebitAccount.Text = accountExpression.Match(this.comboBoxDebitAccount.SelectedItem.ToString()).Value;
        }

        private void comboBoxCreditAccount_SelectionChangeCommitted(object sender, EventArgs e)
        {
            this.textBoxCreditAccount.Text = accountExpression.Match(this.comboBoxCreditAccount.SelectedItem.ToString()).Value;
        }

        private void OnDebitAccountTextChanged(object sender, EventArgs e)
        {
            var accountName = this.comboBoxDebitAccount.Items.Cast<string>().FirstOrDefault(x => x.Contains(this.textBoxDebitAccount.Text));
            if (string.IsNullOrEmpty(accountName))
            {
                return;
            }

            this.comboBoxDebitAccount.SelectedItem = accountName;
        }

        private void OnCreditAccountTextChanged(object sender, EventArgs e)
        {
            var accountName = this.comboBoxCreditAccount.Items.Cast<string>().FirstOrDefault(x => x.Contains(this.textBoxCreditAccount.Text));
            if (string.IsNullOrEmpty(accountName))
            {
                return;
            }

            this.comboBoxCreditAccount.SelectedItem = accountName;
        }
    }
}