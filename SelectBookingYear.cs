// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace lg2de.SimpleAccounting
{
    public partial class SelectBookingYear : Form
    {
        string m_strCurrentYear;
        public SelectBookingYear()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            m_strCurrentYear = (string)comboBoxYears.SelectedItem;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public void AddYear(string strName)
        {
            comboBoxYears.Items.Add( strName );
        }
        public string CurrentYear
        {
            get { return m_strCurrentYear; }
            set
            {
                m_strCurrentYear = value;
                int nPos = comboBoxYears.FindString( m_strCurrentYear );
                if ( nPos >= 0 )
                    comboBoxYears.SelectedIndex = nPos;
                else
                    comboBoxYears.SelectedIndex = comboBoxYears.Items.Count - 1;
            }
        }
    }
}