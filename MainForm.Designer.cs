namespace lg2de.SimpleAccounting
{
    partial class MainForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.MenuItemArchive = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemArchiveOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemArchiveSave = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemArchiveExit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemActions = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemActionsBooking = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemActionSelectYear = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemActionCloseYear = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemReports = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemReportsJournal = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemReportsSummary = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemReportsBilanz = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewJournal = new System.Windows.Forms.ListView();
            this.columnDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnText = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnDebit = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnCredit = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainerTop = new System.Windows.Forms.SplitContainer();
            this.splitContainerAccount = new System.Windows.Forms.SplitContainer();
            this.labelAccountJournal = new System.Windows.Forms.Label();
            this.listViewAccountJournal = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.listViewAccounts = new System.Windows.Forms.ListView();
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).BeginInit();
            this.splitContainerTop.Panel1.SuspendLayout();
            this.splitContainerTop.Panel2.SuspendLayout();
            this.splitContainerTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerAccount)).BeginInit();
            this.splitContainerAccount.Panel1.SuspendLayout();
            this.splitContainerAccount.Panel2.SuspendLayout();
            this.splitContainerAccount.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuMain
            // 
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemArchive,
            this.MenuItemActions,
            this.MenuItemReports});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Size = new System.Drawing.Size(792, 24);
            this.menuMain.TabIndex = 0;
            this.menuMain.Text = "menuStrip1";
            // 
            // MenuItemArchive
            // 
            this.MenuItemArchive.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemArchiveOpen,
            this.MenuItemArchiveSave,
            this.toolStripSeparator1,
            this.MenuItemArchiveExit});
            this.MenuItemArchive.Name = "MenuItemArchive";
            this.MenuItemArchive.Size = new System.Drawing.Size(53, 20);
            this.MenuItemArchive.Text = "Archiv";
            // 
            // MenuItemArchiveOpen
            // 
            this.MenuItemArchiveOpen.Name = "MenuItemArchiveOpen";
            this.MenuItemArchiveOpen.Size = new System.Drawing.Size(180, 22);
            this.MenuItemArchiveOpen.Text = "Öffnen";
            this.MenuItemArchiveOpen.Click += new System.EventHandler(this.MenuItemArchiveOpen_Click);
            // 
            // MenuItemArchiveSave
            // 
            this.MenuItemArchiveSave.Name = "MenuItemArchiveSave";
            this.MenuItemArchiveSave.Size = new System.Drawing.Size(180, 22);
            this.MenuItemArchiveSave.Text = "Speichern";
            this.MenuItemArchiveSave.Click += new System.EventHandler(this.MenuItemArchiveSave_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // MenuItemArchiveExit
            // 
            this.MenuItemArchiveExit.Name = "MenuItemArchiveExit";
            this.MenuItemArchiveExit.Size = new System.Drawing.Size(180, 22);
            this.MenuItemArchiveExit.Text = "Beenden";
            this.MenuItemArchiveExit.Click += new System.EventHandler(this.MenuItemArchiveExit_Click);
            // 
            // MenuItemActions
            // 
            this.MenuItemActions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemActionsBooking,
            this.MenuItemActionSelectYear,
            this.MenuItemActionCloseYear});
            this.MenuItemActions.Name = "MenuItemActions";
            this.MenuItemActions.Size = new System.Drawing.Size(67, 20);
            this.MenuItemActions.Text = "Aktionen";
            // 
            // MenuItemActionsBooking
            // 
            this.MenuItemActionsBooking.Name = "MenuItemActionsBooking";
            this.MenuItemActionsBooking.Size = new System.Drawing.Size(213, 22);
            this.MenuItemActionsBooking.Text = "Buchen";
            this.MenuItemActionsBooking.Click += new System.EventHandler(this.MenuItemActionsBooking_Click);
            // 
            // MenuItemActionSelectYear
            // 
            this.MenuItemActionSelectYear.Name = "MenuItemActionSelectYear";
            this.MenuItemActionSelectYear.Size = new System.Drawing.Size(213, 22);
            this.MenuItemActionSelectYear.Text = "Buchungsjahr wählen";
            this.MenuItemActionSelectYear.Click += new System.EventHandler(this.MenuItemActionsSelectYear_Click);
            // 
            // MenuItemActionCloseYear
            // 
            this.MenuItemActionCloseYear.Name = "MenuItemActionCloseYear";
            this.MenuItemActionCloseYear.Size = new System.Drawing.Size(213, 22);
            this.MenuItemActionCloseYear.Text = "Buchungsjahr abschließen";
            this.MenuItemActionCloseYear.Click += new System.EventHandler(this.MenuItemActionCloseYear_Click);
            // 
            // MenuItemReports
            // 
            this.MenuItemReports.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemReportsJournal,
            this.MenuItemReportsSummary,
            this.MenuItemReportsBilanz});
            this.MenuItemReports.Name = "MenuItemReports";
            this.MenuItemReports.Size = new System.Drawing.Size(62, 20);
            this.MenuItemReports.Text = "Berichte";
            // 
            // MenuItemReportsJournal
            // 
            this.MenuItemReportsJournal.Name = "MenuItemReportsJournal";
            this.MenuItemReportsJournal.Size = new System.Drawing.Size(184, 22);
            this.MenuItemReportsJournal.Text = "Journal";
            this.MenuItemReportsJournal.Click += new System.EventHandler(this.MenuItemReportsJournal_Click);
            // 
            // MenuItemReportsSummary
            // 
            this.MenuItemReportsSummary.Name = "MenuItemReportsSummary";
            this.MenuItemReportsSummary.Size = new System.Drawing.Size(184, 22);
            this.MenuItemReportsSummary.Text = "Summen und Salden";
            this.MenuItemReportsSummary.Click += new System.EventHandler(this.MenuItemReportsSummary_Click);
            // 
            // MenuItemReportsBilanz
            // 
            this.MenuItemReportsBilanz.Name = "MenuItemReportsBilanz";
            this.MenuItemReportsBilanz.Size = new System.Drawing.Size(184, 22);
            this.MenuItemReportsBilanz.Text = "Jahresbilanz";
            this.MenuItemReportsBilanz.Click += new System.EventHandler(this.MenuItemReportsBilanz_Click);
            // 
            // listViewJournal
            // 
            this.listViewJournal.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnDate,
            this.columnNumber,
            this.columnText,
            this.columnValue,
            this.columnDebit,
            this.columnCredit});
            this.listViewJournal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewJournal.FullRowSelect = true;
            this.listViewJournal.GridLines = true;
            this.listViewJournal.HideSelection = false;
            this.listViewJournal.Location = new System.Drawing.Point(0, 0);
            this.listViewJournal.Name = "listViewJournal";
            this.listViewJournal.Size = new System.Drawing.Size(378, 417);
            this.listViewJournal.TabIndex = 1;
            this.listViewJournal.UseCompatibleStateImageBehavior = false;
            this.listViewJournal.View = System.Windows.Forms.View.Details;
            // 
            // columnDate
            // 
            this.columnDate.Text = "Datum";
            this.columnDate.Width = 69;
            // 
            // columnNumber
            // 
            this.columnNumber.Text = "Belegnr.";
            this.columnNumber.Width = 56;
            // 
            // columnText
            // 
            this.columnText.Text = "Buchungstext";
            this.columnText.Width = 110;
            // 
            // columnValue
            // 
            this.columnValue.Text = "Betrag";
            this.columnValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnValue.Width = 53;
            // 
            // columnDebit
            // 
            this.columnDebit.Text = "Sollkonto";
            this.columnDebit.Width = 74;
            // 
            // columnCredit
            // 
            this.columnCredit.Text = "Habenkonto";
            this.columnCredit.Width = 75;
            // 
            // splitContainerTop
            // 
            this.splitContainerTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTop.Location = new System.Drawing.Point(0, 0);
            this.splitContainerTop.Name = "splitContainerTop";
            // 
            // splitContainerTop.Panel1
            // 
            this.splitContainerTop.Panel1.Controls.Add(this.listViewJournal);
            // 
            // splitContainerTop.Panel2
            // 
            this.splitContainerTop.Panel2.Controls.Add(this.splitContainerAccount);
            this.splitContainerTop.Size = new System.Drawing.Size(792, 417);
            this.splitContainerTop.SplitterDistance = 378;
            this.splitContainerTop.TabIndex = 4;
            // 
            // splitContainerAccount
            // 
            this.splitContainerAccount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerAccount.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerAccount.Location = new System.Drawing.Point(0, 0);
            this.splitContainerAccount.Name = "splitContainerAccount";
            this.splitContainerAccount.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerAccount.Panel1
            // 
            this.splitContainerAccount.Panel1.Controls.Add(this.labelAccountJournal);
            // 
            // splitContainerAccount.Panel2
            // 
            this.splitContainerAccount.Panel2.Controls.Add(this.listViewAccountJournal);
            this.splitContainerAccount.Size = new System.Drawing.Size(410, 417);
            this.splitContainerAccount.SplitterDistance = 25;
            this.splitContainerAccount.TabIndex = 5;
            // 
            // labelAccountJournal
            // 
            this.labelAccountJournal.AutoSize = true;
            this.labelAccountJournal.Location = new System.Drawing.Point(4, 4);
            this.labelAccountJournal.Name = "labelAccountJournal";
            this.labelAccountJournal.Size = new System.Drawing.Size(61, 13);
            this.labelAccountJournal.TabIndex = 0;
            this.labelAccountJournal.Text = "Kontenblatt";
            // 
            // listViewAccountJournal
            // 
            this.listViewAccountJournal.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.listViewAccountJournal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewAccountJournal.FullRowSelect = true;
            this.listViewAccountJournal.GridLines = true;
            this.listViewAccountJournal.HideSelection = false;
            this.listViewAccountJournal.Location = new System.Drawing.Point(0, 0);
            this.listViewAccountJournal.Name = "listViewAccountJournal";
            this.listViewAccountJournal.Size = new System.Drawing.Size(410, 388);
            this.listViewAccountJournal.TabIndex = 0;
            this.listViewAccountJournal.UseCompatibleStateImageBehavior = false;
            this.listViewAccountJournal.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Datum";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Belegnr.";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Buchungstext";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Sollwert";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Habenwert";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Gegenkonto";
            this.columnHeader6.Width = 120;
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 24);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerTop);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.listViewAccounts);
            this.splitContainerMain.Size = new System.Drawing.Size(792, 549);
            this.splitContainerMain.SplitterDistance = 417;
            this.splitContainerMain.TabIndex = 2;
            // 
            // listViewAccounts
            // 
            this.listViewAccounts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader7,
            this.columnHeader8});
            this.listViewAccounts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewAccounts.FullRowSelect = true;
            this.listViewAccounts.GridLines = true;
            this.listViewAccounts.HideSelection = false;
            this.listViewAccounts.Location = new System.Drawing.Point(0, 0);
            this.listViewAccounts.Name = "listViewAccounts";
            this.listViewAccounts.Size = new System.Drawing.Size(792, 128);
            this.listViewAccounts.TabIndex = 0;
            this.listViewAccounts.UseCompatibleStateImageBehavior = false;
            this.listViewAccounts.View = System.Windows.Forms.View.Details;
            this.listViewAccounts.DoubleClick += new System.EventHandler(this.listViewAccounts_DoubleClick);
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Nummer";
            this.columnHeader7.Width = 79;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Bezeichnung";
            this.columnHeader8.Width = 438;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 573);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Buchhaltung";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.splitContainerTop.Panel1.ResumeLayout(false);
            this.splitContainerTop.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerTop)).EndInit();
            this.splitContainerTop.ResumeLayout(false);
            this.splitContainerAccount.Panel1.ResumeLayout(false);
            this.splitContainerAccount.Panel1.PerformLayout();
            this.splitContainerAccount.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerAccount)).EndInit();
            this.splitContainerAccount.ResumeLayout(false);
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem MenuItemArchive;
        private System.Windows.Forms.ToolStripMenuItem MenuItemArchiveOpen;
        private System.Windows.Forms.ToolStripMenuItem MenuItemArchiveSave;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MenuItemArchiveExit;
        private System.Windows.Forms.ToolStripMenuItem MenuItemActions;
        private System.Windows.Forms.ToolStripMenuItem MenuItemActionsBooking;
        private System.Windows.Forms.ToolStripMenuItem MenuItemReports;
        private System.Windows.Forms.ToolStripMenuItem MenuItemReportsJournal;
        private System.Windows.Forms.ListView listViewJournal;
        private System.Windows.Forms.ColumnHeader columnDate;
        private System.Windows.Forms.ColumnHeader columnNumber;
        private System.Windows.Forms.ColumnHeader columnText;
        private System.Windows.Forms.ColumnHeader columnValue;
        private System.Windows.Forms.ColumnHeader columnDebit;
        private System.Windows.Forms.ColumnHeader columnCredit;
        private System.Windows.Forms.SplitContainer splitContainerTop;
        private System.Windows.Forms.SplitContainer splitContainerAccount;
        private System.Windows.Forms.ListView listViewAccountJournal;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ToolStripMenuItem MenuItemActionSelectYear;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.Label labelAccountJournal;
        private System.Windows.Forms.ListView listViewAccounts;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ToolStripMenuItem MenuItemReportsSummary;
        private System.Windows.Forms.ToolStripMenuItem MenuItemActionCloseYear;
        private System.Windows.Forms.ToolStripMenuItem MenuItemReportsBilanz;
    }
}

