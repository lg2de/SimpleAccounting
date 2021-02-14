﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using Caliburn.Micro;

    public class JournalItemBaseViewModel : PropertyChangedBase
    {
        private bool isEvenRow;

        protected JournalItemBaseViewModel()
        {
        }

        public ulong Identifier { get; set; }

        public DateTime Date { get; set; }

        public string Text { get; set; } = string.Empty;

        public bool IsFollowup { get; set; }

        public bool IsEvenRow
        {
            get => this.isEvenRow;
            set
            {
                if (value == this.isEvenRow)
                {
                    return;
                }

                this.isEvenRow = value;
                this.NotifyOfPropertyChange();
            }
        }
    }
}