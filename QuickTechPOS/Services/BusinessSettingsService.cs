// File: QuickTechPOS/Services/BusinessSettingsService.cs
using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Service for managing business settings
    /// </summary>
    public class BusinessSettingsService
    {
        private readonly DatabaseContext _dbContext;
        private Dictionary<string, string> _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the business settings service
        /// </summary>
        public BusinessSettingsService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
            _cachedSettings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets a setting by key
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <param name="defaultValue">Default value if the key is not found</param>
        /// <returns>The setting value</returns>
        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            if (_cachedSettings.TryGetValue(key, out string value))
            {
                return value;
            }

            try
            {
                var setting = await _dbContext.BusinessSettings
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                if (setting != null)
                {
                    _cachedSettings[key] = setting.Value;
                    return setting.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting business setting {key}: {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets all business settings
        /// </summary>
        /// <returns>Dictionary of settings</returns>
        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            try
            {
                var settings = await _dbContext.BusinessSettings.ToListAsync();

                _cachedSettings = settings.ToDictionary(s => s.Key, s => s.Value);
                return _cachedSettings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all business settings: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Updates a setting
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <param name="value">The new value</param>
        /// <param name="userId">ID of the user making the change</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateSettingAsync(string key, string value, string userId = "System")
        {
            try
            {
                var setting = await _dbContext.BusinessSettings
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                if (setting != null)
                {
                    setting.Value = value;
                    setting.LastModified = DateTime.Now;
                    setting.ModifiedBy = userId;
                }
                else
                {
                    setting = new BusinessSetting
                    {
                        Key = key,
                        Value = value,
                        Description = $"Setting for {key}",
                        Group = "System",
                        DataType = "string",
                        IsSystem = false,
                        LastModified = DateTime.Now,
                        ModifiedBy = userId
                    };
                    _dbContext.BusinessSettings.Add(setting);
                }

                await _dbContext.SaveChangesAsync();
                _cachedSettings[key] = value;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating business setting {key}: {ex.Message}");
                return false;
            }
        }

        // Common settings accessors

        /// <summary>
        /// Gets the company name
        /// </summary>
        public async Task<string> GetCompanyNameAsync() => await GetSettingAsync("CompanyName", "QuickTech Systems");

        /// <summary>
        /// Gets the company address
        /// </summary>
        public async Task<string> GetAddressAsync() => await GetSettingAsync("Address", "");

        /// <summary>
        /// Gets the company phone number
        /// </summary>
        public async Task<string> GetPhoneAsync() => await GetSettingAsync("Phone", "");

        /// <summary>
        /// Gets the company email
        /// </summary>
        public async Task<string> GetEmailAsync() => await GetSettingAsync("Email", "");

        /// <summary>
        /// Gets the currency symbol
        /// </summary>
        public async Task<string> GetCurrencyAsync() => await GetSettingAsync("Currency", "USD");

        /// <summary>
        /// Gets the tax rate
        /// </summary>
        public async Task<decimal> GetTaxRateAsync()
        {
            var taxRateStr = await GetSettingAsync("TaxRate", "0");
            if (decimal.TryParse(taxRateStr, out decimal taxRate))
            {
                return taxRate;
            }
            return 0;
        }

        /// <summary>
        /// Gets the exchange rate
        /// </summary>
        public async Task<decimal> GetExchangeRateAsync()
        {
            var rateStr = await GetSettingAsync("ExchangeRate", "90000");
            if (decimal.TryParse(rateStr, out decimal rate))
            {
                return rate;
            }
            return 90000;
        }

        /// <summary>
        /// Gets the first receipt footer line
        /// </summary>
        public async Task<string> GetReceiptFooter1Async() => await GetSettingAsync("ReceiptFooter1", "Thank you for your business!");

        /// <summary>
        /// Gets the second receipt footer line
        /// </summary>
        public async Task<string> GetReceiptFooter2Async() => await GetSettingAsync("ReceiptFooter2", "See you next time");
    }
}