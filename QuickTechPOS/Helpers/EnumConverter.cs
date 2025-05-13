// Add this class to the Helpers folder (QuickTechPOS/Helpers/EnumConverter.cs)

using System;
using QuickTechPOS.Models.Enums;

namespace QuickTechPOS.Helpers
{
    /// <summary>
    /// Helper methods for converting between enums and strings
    /// </summary>
    public static class EnumConverter
    {
        /// <summary>
        /// Converts a string to a TransactionStatus enum value
        /// </summary>
        public static TransactionStatus StringToTransactionStatus(string statusString)
        {
            if (string.IsNullOrEmpty(statusString))
                return TransactionStatus.Completed;

            if (Enum.TryParse<TransactionStatus>(statusString, true, out var status))
                return status;

            // Handle legacy string values that might not match exact enum names
            switch (statusString.ToLower().Trim())
            {
                case "pending":
                    return TransactionStatus.Pending;
                case "complete":
                case "completed":
                    return TransactionStatus.Completed;
                case "cancel":
                case "cancelled":
                    return TransactionStatus.Cancelled;
                case "void":
                case "voided":
                    return TransactionStatus.Voided;
                case "refund":
                case "refunded":
                    return TransactionStatus.Refunded;
                default:
                    return TransactionStatus.Completed;
            }
        }

        /// <summary>
        /// Converts a string to a TransactionType enum value
        /// </summary>
        public static TransactionType StringToTransactionType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return TransactionType.Sale;

            if (Enum.TryParse<TransactionType>(typeString, true, out var type))
                return type;

            // Handle legacy string values that might not match exact enum names
            switch (typeString.ToLower().Trim())
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
    }
}