using System;
using System.Collections.Generic;

namespace QuickTechPOS.Models
{
    public class HeldCart
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Description))
                return Description;

            return $"#{Id} - {CustomerName} - {Items.Count} items - ${TotalAmount:F2} - {CreatedAt:g}";
        }
    }
}