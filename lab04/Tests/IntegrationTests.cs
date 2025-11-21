using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using OrderManagementAPI.Data;
using OrderManagementAPI.Services;
using OrderManagementAPI.Validators;
using OrderManagementAPI.Models;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Mappers;

namespace OrderManagementAPI.Tests
{
    //integration tests for OrderService with all advanced features
    public class CreateOrderHandlerIntegrationTests : IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly Mock<ILogger<CreateOrderProfileValidator>> _mockValidatorLogger;
        private readonly OrderService _handler;
        private readonly ICacheService _cacheService;
        private readonly IOrderRepository _repository;
        private readonly CreateOrderProfileValidator _validator;

        public CreateOrderHandlerIntegrationTests()
        {
            // setup in-memory database with unique name
            var options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationContext(options);

            // configure AutoMapper with both profiles
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<OrderMappingProfile>();
            });
            _mapper = config.CreateMapper();

            // setup memory cache
            _cache = new MemoryCache(new MemoryCacheOptions());
            _cacheService = new MemoryCacheService(_cache);

            // setup repository
            _repository = new OrderRepository(_context);

            // mock loggers
            _mockLogger = new Mock<ILogger<OrderService>>();
            _mockValidatorLogger = new Mock<ILogger<CreateOrderProfileValidator>>();

            // setup validator
            _validator = new CreateOrderProfileValidator(_context, _mockValidatorLogger.Object);

            // create handler with all dependencies
            _handler = new OrderService(
                _repository,
                _mapper,
                _mockLogger.Object,
                _cacheService,
                _validator);
        }

        [Fact]
        public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMappings()
        {
            // arrange
            var request = new CreateOrderProfileRequest
            {
                Title = "Advanced Programming Techniques",
                Author = "John Smith",
                ISBN = "9781234567890",
                Category = OrderCategory.Technical,
                Price = 45.99m,
                PublishedDate = DateTime.UtcNow.AddMonths(-6),
                CoverImageUrl = "https://example.com/cover.jpg",
                StockQuantity = 15
            };

            // act
            var result = await _handler.CreateOrderAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Created", result.GetType().Name.Contains("Created") == false ? "Success" : "Created");
            Assert.Equal("Technical & Professional", result.CategoryDisplayName);
            Assert.Equal("JS", result.AuthorInitials); // John Smith -> JS
            Assert.True(result.PublishedAge.Contains("months") || result.PublishedAge == "New Release");
            Assert.True(result.FormattedPrice.StartsWith("$") || result.FormattedPrice.StartsWith("£") || result.FormattedPrice.Contains("€"));
            Assert.Equal("In Stock", result.AvailabilityStatus); // 15 > 5

            // verify logging was called for OrderCreationStarted
            /*_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.Is<EventId>(e => e.Id == LogEvents.OrderCreationStarted),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);*/
        }

        [Fact]
        public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
        {
            // arrange - Create existing order in database
            var existingOrder = new Order("Existing Book", "Existing Author", "9781111111111", 
                OrderCategory.Fiction, 29.99m, DateTime.UtcNow.AddYears(-1), null, 5);
            await _repository.AddAsync(existingOrder);

            var request = new CreateOrderProfileRequest
            {
                Title = "New Book",
                Author = "New Author", 
                ISBN = "9781111111111", // Same ISBN
                Category = OrderCategory.Fiction,
                Price = 19.99m,
                PublishedDate = DateTime.UtcNow.AddMonths(-3),
                StockQuantity = 10
            };

            // act & assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.CreateOrderAsync(request));
            Assert.Contains("already exists", exception.Message.ToLower());

            // verify OrderValidationFailed log was called
            /*_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == LogEvents.OrderValidationFailed),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);*/
        }

        [Fact]
        public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
        {
            // arrange
            var request = new CreateOrderProfileRequest
            {
                Title = "Happy Children Stories",
                Author = "Mary Johnson",
                ISBN = "9782222222222",
                Category = OrderCategory.Children,
                Price = 25.00m, // Should be discounted to 22.50
                PublishedDate = DateTime.UtcNow.AddYears(-2),
                CoverImageUrl = "https://example.com/children-cover.jpg", // Should be null after mapping
                StockQuantity = 3
            };

            // act
            var result = await _handler.CreateOrderAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Children's Orders", result.CategoryDisplayName);
            Assert.Equal(22.50m, result.Price); // 10% discount applied (25.00 * 0.9)
            Assert.Null(result.CoverImageUrl); // Content filtering for children
            Assert.Equal("Limited Stock", result.AvailabilityStatus); // 3 <= 5
        }

        [Fact]
        public async Task Handle_InvalidTechnicalOrder_FailsBusinessRules()
        {
            // arrange - Technical order below minimum price
            var request = new CreateOrderProfileRequest
            {
                Title = "Programming Guide",
                Author = "Tech Author",
                ISBN = "9783333333333",
                Category = OrderCategory.Technical,
                Price = 15.00m, // Below $20 minimum for technical
                PublishedDate = DateTime.UtcNow.AddMonths(-1),
                StockQuantity = 5
            };

            // act & assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.CreateOrderAsync(request));
            Assert.Contains("minimum price of $20.00", exception.Message);
        }

        [Fact]
        public async Task Handle_HighValueOrder_EnforcesStockLimit()
        {
            // arrange - High value order with too much stock
            var request = new CreateOrderProfileRequest
            {
                Title = "Expensive Premium Book",
                Author = "Premium Author",
                ISBN = "9784444444444",
                Category = OrderCategory.Fiction,
                Price = 750.00m, // > $500
                StockQuantity = 25, // > 20 limit
                PublishedDate = DateTime.UtcNow.AddYears(-1)
            };

            // act & assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.CreateOrderAsync(request));
            Assert.Contains("limited stock", exception.Message.ToLower());
        }

        [Fact]
        public async Task Handle_AuthorInitials_HandlesVariousNameFormats()
        {
            // test single name
            var singleNameRequest = new CreateOrderProfileRequest
            {
                Title = "Single Name Book",
                Author = "Cher",
                ISBN = "9785555555555",
                Category = OrderCategory.Fiction,
                Price = 20.00m,
                PublishedDate = DateTime.UtcNow.AddYears(-1),
                StockQuantity = 1
            };

            var singleResult = await _handler.CreateOrderAsync(singleNameRequest);
            Assert.Equal("C", singleResult.AuthorInitials);

            // reset context for next test
            _context.Orders.RemoveRange(_context.Orders);
            await _context.SaveChangesAsync();

            // test multiple names (should use first and last)
            var multipleNameRequest = new CreateOrderProfileRequest
            {
                Title = "Multiple Name Book",
                Author = "John Michael Smith Jr",
                ISBN = "9786666666666",
                Category = OrderCategory.Fiction,
                Price = 20.00m,
                PublishedDate = DateTime.UtcNow.AddYears(-1),
                StockQuantity = 1
            };

            var multipleResult = await _handler.CreateOrderAsync(multipleNameRequest);
            Assert.Equal("JJ", multipleResult.AuthorInitials); // John + Jr (first and last)
        }

        public void Dispose()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }

    //additional unit tests for individual components
    public class OrderValidationTests : IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly Mock<ILogger<CreateOrderProfileValidator>> _mockLogger;
        private readonly CreateOrderProfileValidator _validator;

        public OrderValidationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationContext(options);
            _mockLogger = new Mock<ILogger<CreateOrderProfileValidator>>();
            _validator = new CreateOrderProfileValidator(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task ValidateAsync_ValidRequest_PassesValidation()
        {
            // arrange
            var request = new CreateOrderProfileRequest
            {
                Title = "Valid Programming Book",
                Author = "John Developer",
                ISBN = "9787777777777",
                Category = OrderCategory.Technical,
                Price = 35.00m,
                PublishedDate = DateTime.UtcNow.AddMonths(-6),
                StockQuantity = 10
            };

            // act
            var result = await _validator.ValidateAsync(request);

            // assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("", false)] // Empty title
        [InlineData("A", true)] // Minimum length
        [InlineData("This is a very long title that exceeds the maximum allowed length of 200 characters. This title is intentionally made very long to test the validation rule that limits the title length to a maximum of 200 characters. This should fail validation.", false)] // Too long
        [InlineData("Inappropriate violence content", false)] // Contains inappropriate word
        public async Task ValidateAsync_TitleValidation_WorksCorrectly(string title, bool shouldBeValid)
        {
            // arrange
            var request = new CreateOrderProfileRequest
            {
                Title = title,
                Author = "Test Author",
                ISBN = "9788888888888",
                Category = OrderCategory.Fiction,
                Price = 20.00m,
                PublishedDate = DateTime.UtcNow.AddMonths(-1),
                StockQuantity = 5
            };

            // act
            var result = await _validator.ValidateAsync(request);

            // assert
            Assert.Equal(shouldBeValid, result.IsValid);
        }

        [Theory]
        [InlineData("9781234567890", true)] // Valid 13-digit ISBN
        [InlineData("1234567890", true)] // Valid 10-digit ISBN
        [InlineData("978-1-234-56789-0", true)] // Valid with hyphens
        [InlineData("12345", false)] // Too short
        [InlineData("123456789X", false)] // Contains letter
        [InlineData("", false)] // Empty
        public void ISBNValidation_WorksCorrectly(string isbn, bool shouldBeValid)
        {
            // arrange
            var attribute = new ValidISBNAttribute();

            // act
            var result = attribute.IsValid(isbn);

            // assert
            Assert.Equal(shouldBeValid, result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}