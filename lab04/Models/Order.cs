using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.Models
{
    //represents the category of an order
    public enum OrderCategory
    {
        Fiction = 0,
        NonFiction = 1,
        Technical = 2,
        Children = 3
    }

    //represents an order entity with all required properties
    public class Order
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string ISBN { get; set; } = string.Empty;
        
        public OrderCategory Category { get; set; }
        
        [Range(0.01, 9999.99)]
        public decimal Price { get; set; }
        
        public DateTime PublishedDate { get; set; }
        
        public string? CoverImageUrl { get; set; }
        
        //computed property based on stock quantity
        public bool IsAvailable => StockQuantity > 0;
        
        [Range(0, 100000)]
        public int StockQuantity { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        public Order()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public Order(string title, string author, string isbn, OrderCategory category,
                     decimal price, DateTime publishedDate, string? coverImageUrl = null,
                     int stockQuantity = 0) : this()
        {
            Title = title;
            Author = author;
            ISBN = isbn;
            Category = category;
            Price = price;
            PublishedDate = publishedDate;
            CoverImageUrl = coverImageUrl;
            StockQuantity = stockQuantity;
        }
    }
}