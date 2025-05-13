using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTechPOS.Models.Enums
{
    /// <summary>
    /// Represents the type of transaction
    /// </summary>
    public enum TransactionType
    {
        Sale = 0,
        Return = 1,
        Exchange = 2,
        Void = 3,
        Refund = 4
    }
}