// File: QuickTechPOS/Models/DrawerHistoryEntry.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a historical entry for drawer activity
    /// </summary>
    public class DrawerHistoryEntry
    {
        /// <summary>
        /// Unique identifier for the history entry
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Date and time when the action occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Type of action performed (Open, Close, Cash In, Cash Out, etc.)
        /// </summary>
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Description of the action
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Amount involved in the action
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Drawer balance after this action
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ResultingBalance { get; set; }

        /// <summary>
        /// ID of the user who performed the action
        /// </summary>
        [MaxLength(50)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the formatted amount with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedAmount => $"${Amount:F2}";

        /// <summary>
        /// Gets the formatted balance with currency symbol
        /// </summary>
        [NotMapped]
        public string FormattedResultingBalance => $"${ResultingBalance:F2}";

        /// <summary>
        /// Gets the formatted timestamp
        /// </summary>
        [NotMapped]
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}