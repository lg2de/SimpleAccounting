// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    public partial class BookingValue
    {
        internal BookingValue Clone()
        {
            return this.MemberwiseClone() as BookingValue;
        }
    }
}
