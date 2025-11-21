using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Extensions;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Services
{
    //enhanced CreateOrderHandler with comprehensive logging and performance tracking
    public class OrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly ICacheService _cache;
        private readonly IValidator<CreateOrderProfileRequest> _validator;

        public OrderService(
            IOrderRepository repository,
            IMapper mapper,
            ILogger<OrderService> logger,
            ICacheService cache,
            IValidator<CreateOrderProfileRequest> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _validator = validator;
        }

        //handles order creation with logging and validation
        public async Task<OrderProfileDto> CreateOrderAsync(CreateOrderProfileRequest request, CancellationToken cancellationToken = default)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = Stopwatch.StartNew();
            var validationStopwatch = new Stopwatch();
            var databaseStopwatch = new Stopwatch();
            
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationId"] = operationId,
                ["OrderTitle"] = request.Title,
                ["ISBN"] = request.ISBN,
                ["Category"] = request.Category
            });

            try
            {
                _logger.LogInformation(LogEvents.OrderCreationStarted, 
                    "Starting order creation | Title: {Title} | Author: {Author} | ISBN: {ISBN} | Category: {Category}",
                    request.Title, request.Author, request.ISBN, request.Category);

                // validation phase
                validationStopwatch.Start();
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                validationStopwatch.Stop();

                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning(LogEvents.OrderValidationFailed,
                        "Order validation failed | Errors: {Errors}", errors);
                    
                    var metrics = new OrderCreationMetrics
                    {
                        OperationId = operationId,
                        OrderTitle = request.Title,
                        ISBN = request.ISBN,
                        Category = request.Category,
                        ValidationDuration = validationStopwatch.Elapsed,
                        DatabaseSaveDuration = TimeSpan.Zero,
                        TotalDuration = stopwatch.Elapsed,
                        Success = false,
                        ErrorReason = "Validation failed: " + errors
                    };
                    
                    _logger.LogOrderCreationMetrics(metrics);
                    throw new ValidationException(validationResult.Errors);
                }

                // ISBN Validation Logging
                _logger.LogInformation(LogEvents.ISBNValidationPerformed, 
                    "ISBN validation completed for: {ISBN}", request.ISBN);

                // stock validation logging
                _logger.LogInformation(LogEvents.StockValidationPerformed,
                    "Stock validation completed | Stock: {Stock}", request.StockQuantity);

                // database operations phase
                databaseStopwatch.Start();
                
                _logger.LogInformation(LogEvents.DatabaseOperationStarted,
                    "Starting database operations for order creation");

                var order = _mapper.Map<Order>(request);
                await _repository.AddAsync(order, cancellationToken);

                _logger.LogInformation(LogEvents.DatabaseOperationCompleted,
                    "Database operations completed | OrderId: {OrderId}", order.Id);
                
                databaseStopwatch.Stop();

                // cache operations
                _logger.LogInformation(LogEvents.CacheOperationPerformed,
                    "Invalidating cache key: all_orders");
                await _cache.RemoveAsync("all_orders", cancellationToken);

                // success response
                var result = _mapper.Map<OrderProfileDto>(order);
                stopwatch.Stop();

                var successMetrics = new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = request.Category,
                    ValidationDuration = validationStopwatch.Elapsed,
                    DatabaseSaveDuration = databaseStopwatch.Elapsed,
                    TotalDuration = stopwatch.Elapsed,
                    Success = true,
                    ErrorReason = null
                };

                _logger.LogOrderCreationMetrics(successMetrics);
                
                _logger.LogInformation(LogEvents.OrderCreationCompleted,
                    "Order creation completed successfully | OrderId: {OrderId}", order.Id);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var errorMetrics = new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = request.Category,
                    ValidationDuration = validationStopwatch.Elapsed,
                    DatabaseSaveDuration = databaseStopwatch.Elapsed,
                    TotalDuration = stopwatch.Elapsed,
                    Success = false,
                    ErrorReason = ex.Message
                };

                _logger.LogOrderCreationMetrics(errorMetrics);
                
                _logger.LogError(ex, "Error creating order | Title: {Title} | ISBN: {ISBN}", 
                    request.Title, request.ISBN);
                
                throw;
            }
        }

        //get all orders
        public async Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
    }
}