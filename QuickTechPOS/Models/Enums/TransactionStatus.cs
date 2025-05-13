using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTechPOS.Models.Enums
{
    /// <summary>
    /// Represents the status of a transaction
    /// </summary>
    public enum TransactionStatus
    {
        Pending = 0,
        Completed = 1,
        Cancelled = 2,
        Voided = 3,
        Refunded = 4
    }
}