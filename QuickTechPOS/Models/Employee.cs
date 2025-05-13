using System;

namespace QuickTechPOS.Models
{
    /// <summary>
    /// Represents an employee in the POS system
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Unique identifier for the employee
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// Username for login purposes
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Hashed password for secure authentication
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Employee's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Employee's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Employee's role in the organization (e.g., Admin, Cashier)
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Indicates if the employee account is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Timestamp when the account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of the last successful login
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Employee's monthly salary
        /// </summary>
        public decimal MonthlySalary { get; set; }

        /// <summary>
        /// Employee's current balance
        /// </summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Gets the full name of the employee
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";
    }
}