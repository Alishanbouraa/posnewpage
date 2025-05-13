// File: QuickTechPOS/Services/DatabaseDiagnostics.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides database diagnostics for troubleshooting
    /// </summary>
    public static class DatabaseDiagnostics
    {
        /// <summary>
        /// Configures a DbContext for detailed logging
        /// </summary>
        /// <param name="optionsBuilder">The DbContext options builder</param>
        public static void EnableSqlLogging(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .LogTo(message => Console.WriteLine($"[DB] {message}"),
                       new[] { DbLoggerCategory.Database.Command.Name },
                       LogLevel.Information);
        }
    }
}