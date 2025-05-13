using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.IO;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides access to application configuration settings
    /// </summary>
    public class ConfigurationService
    {
        private static readonly Lazy<IConfiguration> _configuration = new Lazy<IConfiguration>(() =>
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        });

        /// <summary>
        /// Gets the application configuration
        /// </summary>
        public static IConfiguration Configuration => _configuration.Value;

        /// <summary>
        /// Gets the database connection string
        /// </summary>
        public static string ConnectionString => Configuration.GetConnectionString("DefaultConnection");
    }
}