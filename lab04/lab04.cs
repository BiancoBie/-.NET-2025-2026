using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public enum OrderCategory
{
    Fiction = 0,
    NonFiction = 1,
    Technical = 2,
    Children = 3
}

public class Order
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public OrderCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? CoverImageURL { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }

    public Order(string title, string author, string isbn, OrderCategory category,
                 decimal price, DateTime publishedDate, string? coverImageURL,
                 bool isAvailable, int stockQuantity)
    {
        Title = title;
        Author = author;
        ISBN = isbn;
        Category = category;
        Price = price;
        PublishedDate = publishedDate;
        CoverImageURL = coverImageURL;
        IsAvailable = isAvailable;
        StockQuantity = stockQuantity;
    }
}

class OrderProfileDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public string CategoryDisplayName { get; set; }
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageURL { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string PublishedAge { get; set; }
    public string AuthorInitials { get; set; }
    public string AvailabilityStatus { get; set; }
}

class CreateOrderProfileRequest
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public OrderCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? CoverImageURL { get; set; }
    public int StockQuantity { get; set; }
}

    public class AdvancedOrderMappingProfile : Profile
    {
        public AdvancedOrderMappingProfile()
        {
            // CreateOrderProfileRequest -> Order mapping
            CreateMap<CreateOrderProfileRequest, Order>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
                .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src => src.PublishedDate))
                .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Order -> OrderProfileDto mapping
            CreateMap<Order, OrderProfileDto>()
                .ForMember(dest => dest.CategoryDisplay, opt => opt.MapFrom<CategoryDisplayResolver>())
                .ForMember(dest => dest.PriceDisplay, opt => opt.MapFrom<PriceFormatterResolver>())
                .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
                .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
                .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
        }
    }

    // resolvers
    public class CategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Category switch
            {
                OrderCategory.Fiction => "Fiction & Literature",
                OrderCategory.NonFiction => "Non-Fiction",
                OrderCategory.Technical => "Technical & Professional",
                OrderCategory.Children => "Children's Orders",
                _ => "Invalid Category"
            };
        }
    }

    public class PriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Price.ToString("C2");
        }
    }

    public class PublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            int daysSincePublished = (DateTime.Now - source.PublishedDate).Days;
        if (daysSincePublished < 30)
        {
            return $"New Release";
        }
        else if (daysSincePublished < 365)
        {
            int months = daysSincePublished / 30;
            return $"{months} months old";
        }
        else if (daysSincePublished < 1825)
        {
            int years = daysSincePublished / 365;
            return $"{years} years old";
        }
            else if (daysSincePublished == 1825)
        {
            return $"Classic";
        }
        }
    }

    public class AuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source.Author)) return string.Empty;
            string[] parts = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string initials = "";
            foreach (var p in parts)
            {
                initials += p[0];
            }
            return initials.ToUpper();
        }
    }

public class AvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock";
        }
        else if (source.StockQuantity == 0)
        {
            return "Unavailable";
        }
        else if (source.StockQuantity == 1)
        {
            return "Last Copy";
        }
        else if (source.StockQuantity <= 5)
        {
            return "Limited Stock";
        }
        else
        {
            return "In Stock";
        }
    }

    public class AdvancedOrderMappingProfile : Profile
    {
        public AdvancedOrderMappingProfile()
        {
            CreateMap<CreateOrderProfileRequest, Order>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())

                // CoverImageUrl conditional mapping
                .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                    src.Category == OrderCategory.Children ? null : src.CoverImageUrl))

                // Price conditional mapping
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                    src.Category == OrderCategory.Children ? src.Price * 0.9m : src.Price));

            CreateMap<Order, OrderProfileDto>()
                .ForMember(dest => dest.CategoryDisplay, opt => opt.MapFrom<CategoryDisplayResolver>())
                .ForMember(dest => dest.PriceDisplay, opt => opt.MapFrom<PriceFormatterResolver>())
                .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
                .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
                .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
        }
    }

    //order handler
    public class CreateOrderHandler
    {
        private readonly IOrderRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateOrderHandler> _logger;
        private readonly ICacheService _cache;

        public CreateOrderHandler(
            IOrderRepository repository,
            IMapper mapper,
            ILogger<CreateOrderHandler> logger,
            ICacheService cache)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<OrderProfileDto> HandleAsync(CreateOrderProfileRequest request)
        {
            try
            {
                _logger.LogInformation("Creating order: Title={Title}, Author={Author}, Category={Category}, ISBN={ISBN}",
                    request.Title, request.Author, request.Category, request.ISBN);

                // validation
                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ISBN))
                    throw new ArgumentException("Title and ISBN are required.");

                // check ISBN for no duplicates
                var existing = await _repository.GetByISBNAsync(request.ISBN);
                if (existing != null)
                    throw new InvalidOperationException("An order with this ISBN already exists.");

                //mapping
                var order = _mapper.Map<Order>(request);

                //save
                await _repository.AddAsync(order);

                //cache update with requested keys
                await _cache.RemoveAsync("all_orders");

                //return mapped dto
                var result = _mapper.Map<OrderProfileDto>(order);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for ISBN={ISBN}", request.ISBN);
                throw;
            }
        }
    }
    public interface IOrderRepository
    {
        Task<Order?> GetByISBNAsync(string isbn);
        Task AddAsync(Order order);
    }

    public interface ICacheService
    {
        Task RemoveAsync(string key);
    }
}

//module 2
class LogEvents
{
    public const int OrderCreationStarted = 2001;
    public const int OrderValidationFailed = 2002;
    public const int OrderCreationCompleted = 2003;

    public const int DatabaseOperationStarted = 2004;
    public const int DatabaseOperationCompleted = 2005;
    public const int CacheOperationPerformed = 2006;
    public const int ISBNValidationPerformed = 2007;
    public const int StockValidationPerformed = 2008;
}

record OrderCreationMetrics
{
    public string OperationId { get; init; } = Guid.NewGuid().ToString();
    public string OrderTitle { get; init; } = "";
    public string ISBN { get; init; } = "";
    public OrderCategory Category { get; init; } = OrderCategory.Fiction;
    public TimeSpan ValidationDuration { get; init; }
    public TimeSpan DatabaseSaveDuration { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public bool Success { get; init; }
    public string? ErrorReason { get; init; }
}

public static class LoggingExceptions
{
    public static void LogOrderCreationMetrics(
        this ILogger logger, OrderCreationMetrics metrics)
    {
        if (logger == null || metrics == null) return;
        var orderID = new OrderID(LogEvents.OrderCreationCompleted, "OrderCreationMetrics");
        logger.LogInformation(orderID,
    "Order Metrics | Title: {Title} | ISBN: {ISBN} | Category: {Category} | " +
    "Validation: {ValidationMs} ms | Database Save: {DbSaveMs} ms | Total: {TotalMs} ms | " +
    "Status: {Status} | Error: {ErrorReason}",
    metrics.OrderTitle,
    metrics.ISBN,
    metrics.Category,
    metrics.ValidationDuration.TotalMilliseconds,
    metrics.DatabaseSaveDuration.TotalMilliseconds,
    metrics.TotalDuration.TotalMilliseconds,
    metrics.Success ? "Success" : "Failed",
    metrics.ErrorReason ?? "None");
    }
}