// File: QuickTechPOS/Services/CustomerProductPriceService.cs

using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides operations for managing customer-specific product prices
    /// </summary>
    public class CustomerProductPriceService
    {
        private readonly DatabaseContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the customer product price service
        /// </summary>
        public CustomerProductPriceService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
        }

        /// <summary>
        /// Gets the special price for a specific customer-product combination
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="productId">The product ID</param>
        /// <returns>The special price if it exists, null otherwise</returns>
        public async Task<decimal?> GetCustomerProductPriceAsync(int customerId, int productId)
        {
            try
            {
                if (customerId <= 0)
                    return null;

                var customerPrice = await _dbContext.CustomerProductPrices
                    .FirstOrDefaultAsync(cpp => cpp.CustomerId == customerId &&
                                               cpp.ProductId == productId);

                return customerPrice?.Price;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCustomerProductPriceAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all special prices for a specific customer
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>A dictionary of product IDs to special prices</returns>
        public async Task<Dictionary<int, decimal>> GetAllPricesForCustomerAsync(int customerId)
        {
            try
            {
                if (customerId <= 0)
                    return new Dictionary<int, decimal>();

                var customerPrices = await _dbContext.CustomerProductPrices
                    .Where(cpp => cpp.CustomerId == customerId)
                    .ToDictionaryAsync(cpp => cpp.ProductId, cpp => cpp.Price);

                return customerPrices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPricesForCustomerAsync: {ex.Message}");
                return new Dictionary<int, decimal>();
            }
        }

        /// <summary>
        /// Sets or updates a special price for a customer-product combination
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="productId">The product ID</param>
        /// <param name="price">The special price</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SetCustomerProductPriceAsync(int customerId, int productId, decimal price)
        {
            try
            {
                var existingPrice = await _dbContext.CustomerProductPrices
                    .FirstOrDefaultAsync(cpp => cpp.CustomerId == customerId &&
                                              cpp.ProductId == productId);

                if (existingPrice != null)
                {
                    // Update existing price
                    existingPrice.Price = price;
                }
                else
                {
                    // Create new customer-specific price
                    _dbContext.CustomerProductPrices.Add(new CustomerProductPrice
                    {
                        CustomerId = customerId,
                        ProductId = productId,
                        Price = price
                    });
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetCustomerProductPriceAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a special price for a customer-product combination
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="productId">The product ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteCustomerProductPriceAsync(int customerId, int productId)
        {
            try
            {
                var existingPrice = await _dbContext.CustomerProductPrices
                    .FirstOrDefaultAsync(cpp => cpp.CustomerId == customerId &&
                                              cpp.ProductId == productId);

                if (existingPrice == null)
                    return false;

                _dbContext.CustomerProductPrices.Remove(existingPrice);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteCustomerProductPriceAsync: {ex.Message}");
                return false;
            }
        }
    }
}