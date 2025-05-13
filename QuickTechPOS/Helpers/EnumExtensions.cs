// File: QuickTechPOS/Helpers/EnumExtensions.cs
using QuickTechPOS.Models.Enums;
using System;

namespace QuickTechPOS.Helpers
{
    /// <summary>
    /// Extension methods for enum types
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Converts a string to a TransactionType enum value
        /// </summary>
        /// <param name="typeString">The string representation of a transaction type</param>
        /// <returns>The corresponding TransactionType enum value</returns>
        public static TransactionType ToTransactionType(this string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return TransactionType.Sale;

            if (Enum.TryParse<TransactionType>(typeString, true, out var result))
                return result;

            switch (typeString.ToLower())
            {
                case "sale":
                    return TransactionType.Sale;
                case "return":
                    return TransactionType.Return;
                case "exchange":
                    return TransactionType.Exchange;
                case "void":
                    return TransactionType.Void;
                case "refund":
                    return TransactionType.Refund;
                default:
                    return TransactionType.Sale;
            }
        }

        /// <summary>
        /// Converts a string to a TransactionStatus enum value
        /// </summary>
        /// <param name="statusString">The string representation of a transaction status</param>
        /// <returns>The corresponding TransactionStatus enum value</returns>
        public static TransactionStatus ToTransactionStatus(this string statusString)
        {
            if (string.IsNullOrEmpty(statusString))
                return TransactionStatus.Completed;

            if (Enum.TryParse<TransactionStatus>(statusString, true, out var result))
                return result;

            switch (statusString.ToLower())
            {
                case "pending":
                    return TransactionStatus.Pending;
                case "completed":
                    return TransactionStatus.Completed;
                case "cancelled":
                    return TransactionStatus.Cancelled;
                case "voided":
                    return TransactionStatus.Voided;
                case "refunded":
                    return TransactionStatus.Refunded;
                default:
                    return TransactionStatus.Completed;
            }
        }

        /// <summary>
        /// Gets a display name for a TransactionType enum value
        /// </summary>
        /// <param name="type">The TransactionType enum value</param>
        /// <returns>A user-friendly display name</returns>
        public static string GetDisplayName(this TransactionType type)
        {
            return type switch
            {
                TransactionType.Sale => "Sale",
                TransactionType.Return => "Return",
                TransactionType.Exchange => "Exchange",
                TransactionType.Void => "Void",
                TransactionType.Refund => "Refund",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Gets a display name for a TransactionStatus enum value
        /// </summary>
        /// <param name="status">The TransactionStatus enum value</param>
        /// <returns>A user-friendly display name</returns>
        public static string GetDisplayName(this TransactionStatus status)
        {
            return status switch
            {
                TransactionStatus.Pending => "Pending",
                TransactionStatus.Completed => "Completed",
                TransactionStatus.Cancelled => "Cancelled",
                TransactionStatus.Voided => "Voided",
                TransactionStatus.Refunded => "Refunded",
                _ => status.ToString()
            };
        }
    }
}