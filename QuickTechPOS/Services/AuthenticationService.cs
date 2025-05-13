using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Helpers;
using QuickTechPOS.Models;
using System;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Handles user authentication operations
    /// </summary>
    public class AuthenticationService
    {
        private readonly DatabaseContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the authentication service
        /// </summary>
        public AuthenticationService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
        }

        /// <summary>
        /// Currently authenticated employee
        /// </summary>
        public Employee CurrentEmployee { get; private set; }

        /// <summary>
        /// Authenticates a user with the given credentials
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password (plaintext)</param>
        /// <returns>True if authentication was successful, otherwise false</returns>
        // File: QuickTechPOS/Services/AuthenticationService.cs

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                var employee = await _dbContext.Employees
                    .FirstOrDefaultAsync(e => e.Username == username && e.IsActive);

                if (employee == null)
                    return false;

                // Verify password using our flexible PasswordHasher
                if (!PasswordHasher.VerifyPassword(password, employee.PasswordHash))
                    return false;

                // REMOVE OR COMMENT OUT THESE LINES TO PREVENT PASSWORD HASH UPGRADING
                // If using legacy hash format, upgrade to BCrypt
                /*if (PasswordHasher.NeedsUpgrade(employee.PasswordHash))
                {
                    employee.PasswordHash = PasswordHasher.HashPassword(password);
                }*/

                // Update last login time
                employee.LastLogin = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                // Set current employee
                CurrentEmployee = employee;
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logs out the current user
        /// </summary>
        public void Logout()
        {
            CurrentEmployee = null;
        }
    }
}