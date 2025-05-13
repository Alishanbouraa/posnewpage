// File: QuickTechPOS/Services/DatabaseService.cs

using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides database management services
    /// </summary>
    internal class DatabaseService
    {
        private readonly DatabaseContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the database service
        /// </summary>
        public DatabaseService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
        }

        /// <summary>
        /// Ensures a default walk-in customer exists in the database
        /// </summary>
        public async Task<Customer> EnsureWalkInCustomerExistsAsync()
        {
            try
            {
                Console.WriteLine("Checking for walk-in customer in database...");

                // Check if the walk-in customer already exists
                var walkInCustomer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Name == "Walk-in Customer");

                if (walkInCustomer == null)
                {
                    Console.WriteLine("Walk-in customer not found. Creating new record...");

                    // Create a default walk-in customer
                    walkInCustomer = new Customer
                    {
                        Name = "Walk-in Customer",
                        Phone = "0000000000", // Use a dummy phone number since it's required
                        Email = "",
                        Address = "",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        Balance = 0
                    };

                    _dbContext.Customers.Add(walkInCustomer);
                    await _dbContext.SaveChangesAsync();

                    Console.WriteLine($"Created walk-in customer with ID: {walkInCustomer.CustomerId}");
                    return walkInCustomer;
                }
                else
                {
                    Console.WriteLine($"Walk-in customer already exists with ID: {walkInCustomer.CustomerId}");
                    return walkInCustomer;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring walk-in customer exists: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the ID of the walk-in customer
        /// </summary>
        /// <returns>The walk-in customer ID, or 0 if not found</returns>
        public async Task<int> GetWalkInCustomerIdAsync()
        {
            try
            {
                var walkInCustomer = await _dbContext.Customers
                    .Where(c => c.Name == "Walk-in Customer" && c.IsActive)
                    .FirstOrDefaultAsync();

                return walkInCustomer?.CustomerId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting walk-in customer ID: {ex.Message}");
                return 0;
            }
        }
    }
}