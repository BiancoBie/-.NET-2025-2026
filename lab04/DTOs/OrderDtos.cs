using OrderManagementAPI.Models;

namespace OrderManagementAPI.DTOs
{
    /// <summary>
    /// Data Transfer Object for Order Profile information
    /// </summary>
    public class OrderProfileDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string CategoryDisplayName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string FormattedPrice { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public string PublishedAge { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for creating a new Order Profile
    /// </summary>
    public class CreateOrderProfileRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public OrderCategory Category { get; set; }
        public decimal Price { get; set; }
        public DateTime PublishedDate { get; set; }
        public string? CoverImageUrl { get; set; }
        public int StockQuantity { get; set; } = 1;
    }
}