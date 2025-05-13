// File: QuickTechPOS/Services/ProductService.cs

using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides operations for managing products
    /// </summary>
    public class ProductService
    {
        private readonly DatabaseContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the product service
        /// </summary>
        public ProductService()
        {
            _dbContext = new DatabaseContext(ConfigurationService.ConnectionString);
        }

        /// <summary>
        /// Searches for products by name
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>A list of matching products</returns>
        public async Task<List<Product>> SearchByNameAsync(string query, int maxResults = 10)
        {
            try
            {
                Console.WriteLine($"Searching for products with query: '{query}'");

                if (string.IsNullOrWhiteSpace(query))
                {
                    // Return all products if query is empty
                    var allProducts = await _dbContext.Products
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.Name)
                        .Take(maxResults)
                        .ToListAsync();

                    Console.WriteLine($"Found {allProducts.Count} active products for empty query");
                    return allProducts;
                }

                // Safe search using EF.Functions.Like with null protection
                var products = await _dbContext.Products
                    .Where(p => p.IsActive && p.Name != null && p.Name.Contains(query))
                    .OrderBy(p => p.Name)
                    .Take(maxResults)
                    .ToListAsync();

                Console.WriteLine($"Found {products.Count} products matching '{query}'");
                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchByNameAsync: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return new List<Product>();
            }
        }

        public async Task<ProductSearchResult> FindByAnyBarcodeAsync(string barcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                    return null;

                // First check if it matches a box barcode
                var boxProduct = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.IsActive && p.BoxBarcode == barcode);

                if (boxProduct != null)
                {
                    return new ProductSearchResult
                    {
                        Product = boxProduct,
                        IsBoxBarcode = true
                    };
                }

                // Then check if it matches a regular barcode
                var regularProduct = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.IsActive && p.Barcode == barcode);

                if (regularProduct != null)
                {
                    return new ProductSearchResult
                    {
                        Product = regularProduct,
                        IsBoxBarcode = false
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FindByAnyBarcodeAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        public class ProductSearchResult
        {
            public Product Product { get; set; }
            public bool IsBoxBarcode { get; set; }
        }
        /// <summary>
        /// Updates the box stock of a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="boxQuantitySold">The quantity of boxes sold</param>
        /// <returns>True if the update was successful, otherwise false</returns>
        public async Task<bool> UpdateBoxStockAsync(int productId, decimal boxQuantitySold)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(productId);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {productId} not found during box stock update");
                    return false;
                }

                // Convert decimal box quantity to int (round down)
                int wholeBoxesQuantity = (int)Math.Floor(boxQuantitySold);

                // Calculate items per box
                int itemsPerBox = product.ItemsPerBox > 0 ? product.ItemsPerBox : 1;

                // Calculate total individual items being sold
                decimal totalIndividualItems = boxQuantitySold * itemsPerBox;

                // Update box inventory
                if (product.NumberOfBoxes < wholeBoxesQuantity)
                {
                    Console.WriteLine($"Warning: Insufficient box stock for product {productId}. Available: {product.NumberOfBoxes}, Requested: {wholeBoxesQuantity}");
                    product.NumberOfBoxes = 0;
                }
                else
                {
                    product.NumberOfBoxes -= wholeBoxesQuantity;
                }

                // IMPORTANT: Also update the individual items stock
                if (product.CurrentStock < totalIndividualItems)
                {
                    Console.WriteLine($"Warning: Insufficient item stock for product {productId}. Available: {product.CurrentStock}, Needed: {totalIndividualItems}");
                    product.CurrentStock = 0;
                }
                else
                {
                    product.CurrentStock -= totalIndividualItems;
                }

                product.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Updated box stock for product #{productId}: Boxes={product.NumberOfBoxes}, Items={product.CurrentStock}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateBoxStockAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            try
            {
                return await _dbContext.Products.FindAsync(productId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving product: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates the stock of a product
        /// </summary>
        /// <param name="productId">The product ID</param>
        /// <param name="quantitySold">The quantity sold</param>
        /// <returns>True if the update was successful, otherwise false</returns>
        public async Task<bool> UpdateStockAsync(int productId, decimal quantitySold)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(productId);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {productId} not found during stock update");
                    return false;
                }

                // Check if we have enough stock before updating
                if (product.CurrentStock < quantitySold)
                {
                    Console.WriteLine($"Warning: Insufficient stock for product {productId}. Available: {product.CurrentStock}, Requested: {quantitySold}");
                    // Depending on your business logic, you might want to:
                    // 1. Allow negative stock (remove this check)
                    // 2. Set stock to 0 instead of going negative
                    // 3. Fail the update (return false)

                    // For now, we'll set it to 0 if it would go negative
                    product.CurrentStock = 0;
                }
                else
                {
                    product.CurrentStock -= quantitySold;
                }

                product.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateStockAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Checks if the database contains any product with the specified name
        /// </summary>
        /// <param name="productName">The product name to check</param>
        /// <returns>True if the product exists, otherwise false</returns>
        public async Task<bool> ProductExistsAsync(string productName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName))
                    return false;

                return await _dbContext.Products
                    .AnyAsync(p => p.IsActive && p.Name != null && p.Name.Contains(productName));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProductExistsAsync: {ex.Message}");
                return false;
            }
        }
    }
}