// File: QuickTechPOS/Models/BusinessSetting.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents a business setting configuration
    /// </summary>
    public class BusinessSetting
    {
        /// <summary>
        /// Unique identifier for the setting
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Setting key name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Setting value
        /// </summary>
        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Description of the setting
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Group category for the setting
        /// </summary>
        [MaxLength(100)]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Data type of the setting (string, int, decimal, etc.)
        /// </summary>
        [MaxLength(50)]
        public string DataType { get; set; } = "string";

        /// <summary>
        /// Indicates if this is a system setting
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// Date and time when the setting was last modified
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// User who last modified the setting
        /// </summary>
        [MaxLength(100)]
        public string ModifiedBy { get; set; } = string.Empty;
    }
}