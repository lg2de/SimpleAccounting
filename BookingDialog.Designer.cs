namespace lg2de.SimpleAccounting
{
    partial class BookingDialog
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonBook = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.textBoxBookIdent = new System.Windows.Forms.TextBox();
            this.textValue = new System.Windows.Forms.TextBox();
            this.dateBookDate = new System.Windows.Forms.DateTimePicker();
            this.comboBookText = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonSplitDebit = new System.Windows.Forms.Button();
            this.buttonSplitCredit = new System.Windows.Forms.Button();
            this.comboBoxCreditAccount = new System.Windows.Forms.ComboBox();
            this.comboBoxDebitAccount = new System.Windows.Forms.ComboBox();
            this.textBoxDebitAccount = new System.Windows.Forms.TextBox();
            this.textBoxCreditAccount = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonBook
            // 
            this.buttonBook.Location = new System.Drawing.Point(268, 306);
            this.buttonBook.Name = "buttonBook";
            this.buttonBook.Size = new System.Drawing.Size(75, 23);
            this.buttonBook.TabIndex = 14;
            this.buttonBook.Text = "Buchen";
            this.buttonBook.UseVisualStyleBackColor = true;
            this.buttonBook.Click += new System.EventHandler(this.OnBookClick);
            // 
            // buttonClear
            // 
            this.buttonClear.Enabled = false;
            this.buttonClear.Location = new System.Drawing.Point(349, 306);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(75, 23);
            this.buttonClear.TabIndex = 15;
            this.buttonClear.Text = "Löschen";
            this.buttonClear.UseVisualStyleBackColor = true;
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(430, 306);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 23);
            this.buttonClose.TabIndex = 16;
            this.buttonClose.Text = "Schließen";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.OnClickClose);
            // 
            // textBoxBookIdent
            // 
            this.textBoxBookIdent.Location = new System.Drawing.Point(379, 29);
            this.textBoxBookIdent.Name = "textBoxBookIdent";
            this.textBoxBookIdent.Size = new System.Drawing.Size(100, 20);
            this.textBoxBookIdent.TabIndex = 2;
            // 
            // textValue
            // 
            this.textValue.Location = new System.Drawing.Point(379, 66);
            this.textValue.Name = "textValue";
            this.textValue.Size = new System.Drawing.Size(100, 20);
            this.textValue.TabIndex = 5;
            // 
            // dateBookDate
            // 
            this.dateBookDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateBookDate.Location = new System.Drawing.Point(23, 29);
            this.dateBookDate.Name = "dateBookDate";
            this.dateBookDate.Size = new System.Drawing.Size(98, 20);
            this.dateBookDate.TabIndex = 0;
            // 
            // comboBookText
            // 
            this.comboBookText.FormattingEnabled = true;
            this.comboBookText.Items.AddRange(new object[] {
            "Fahrtkosten",
            "Speisen",
            "Webpräsenz",
            "AKD Zentralmittel"});
            this.comboBookText.Location = new System.Drawing.Point(23, 65);
            this.comboBookText.Name = "comboBookText";
            this.comboBookText.Size = new System.Drawing.Size(222, 21);
            this.comboBookText.TabIndex = 3;
            this.comboBookText.SelectionChangeCommitted += new System.EventHandler(this.OnTextSelectionChangeCommitted);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 112);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Soll";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 169);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Haben";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(292, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Belegnummer";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(333, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(30, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Wert";
            // 
            // buttonSplitDebit
            // 
            this.buttonSplitDebit.Location = new System.Drawing.Point(66, 132);
            this.buttonSplitDebit.Name = "buttonSplitDebit";
            this.buttonSplitDebit.Size = new System.Drawing.Size(75, 23);
            this.buttonSplitDebit.TabIndex = 9;
            this.buttonSplitDebit.Text = "Splitten";
            this.buttonSplitDebit.UseVisualStyleBackColor = true;
            // 
            // buttonSplitCredit
            // 
            this.buttonSplitCredit.Location = new System.Drawing.Point(66, 188);
            this.buttonSplitCredit.Name = "buttonSplitCredit";
            this.buttonSplitCredit.Size = new System.Drawing.Size(75, 23);
            this.buttonSplitCredit.TabIndex = 13;
            this.buttonSplitCredit.Text = "Splitten";
            this.buttonSplitCredit.UseVisualStyleBackColor = true;
            // 
            // comboBoxCreditAccount
            // 
            this.comboBoxCreditAccount.FormattingEnabled = true;
            this.comboBoxCreditAccount.Location = new System.Drawing.Point(172, 161);
            this.comboBoxCreditAccount.Name = "comboBoxCreditAccount";
            this.comboBoxCreditAccount.Size = new System.Drawing.Size(264, 21);
            this.comboBoxCreditAccount.TabIndex = 11;
            this.comboBoxCreditAccount.SelectionChangeCommitted += new System.EventHandler(this.comboBoxCreditAccount_SelectionChangeCommitted);
            // 
            // comboBoxDebitAccount
            // 
            this.comboBoxDebitAccount.FormattingEnabled = true;
            this.comboBoxDebitAccount.Location = new System.Drawing.Point(172, 104);
            this.comboBoxDebitAccount.Name = "comboBoxDebitAccount";
            this.comboBoxDebitAccount.Size = new System.Drawing.Size(264, 21);
            this.comboBoxDebitAccount.TabIndex = 7;
            this.comboBoxDebitAccount.SelectionChangeCommitted += new System.EventHandler(this.OnDebitAccountSelectionChangeCommitted);
            // 
            // textBoxDebitAccount
            // 
            this.textBoxDebitAccount.Location = new System.Drawing.Point(66, 104);
            this.textBoxDebitAccount.Name = "textBoxDebitAccount";
            this.textBoxDebitAccount.Size = new System.Drawing.Size(100, 20);
            this.textBoxDebitAccount.TabIndex = 17;
            this.textBoxDebitAccount.TextChanged += new System.EventHandler(this.OnDebitAccountTextChanged);
            // 
            // textBoxCreditAccount
            // 
            this.textBoxCreditAccount.Location = new System.Drawing.Point(66, 161);
            this.textBoxCreditAccount.Name = "textBoxCreditAccount";
            this.textBoxCreditAccount.Size = new System.Drawing.Size(100, 20);
            this.textBoxCreditAccount.TabIndex = 18;
            this.textBoxCreditAccount.TextChanged += new System.EventHandler(this.OnCreditAccountTextChanged);
            // 
            // BookingDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(517, 346);
            this.Controls.Add(this.textBoxCreditAccount);
            this.Controls.Add(this.textBoxDebitAccount);
            this.Controls.Add(this.comboBoxDebitAccount);
            this.Controls.Add(this.comboBoxCreditAccount);
            this.Controls.Add(this.buttonSplitCredit);
            this.Controls.Add(this.buttonSplitDebit);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBookText);
            this.Controls.Add(this.dateBookDate);
            this.Controls.Add(this.textValue);
            this.Controls.Add(this.textBoxBookIdent);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.buttonBook);
            this.Name = "BookingDialog";
            this.Text = "BookingDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonBook;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.TextBox textBoxBookIdent;
        private System.Windows.Forms.TextBox textValue;
        private System.Windows.Forms.DateTimePicker dateBookDate;
        private System.Windows.Forms.ComboBox comboBookText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonSplitDebit;
        private System.Windows.Forms.Button buttonSplitCredit;
        private System.Windows.Forms.ComboBox comboBoxCreditAccount;
        private System.Windows.Forms.ComboBox comboBoxDebitAccount;
        private System.Windows.Forms.TextBox textBoxDebitAccount;
        private System.Windows.Forms.TextBox textBoxCreditAccount;
    }
}