using Microsoft.Extensions.Logging;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Extensions
{
    /// <summary>
    /// Event IDs for structured logging
    /// </summary>
    public static class LogEvents
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

    /// <summary>
    /// Metrics record for order creation operations
    /// </summary>
    public record OrderCreationMetrics
    {
        public string OperationId { get; init; } = string.Empty;
        public string OrderTitle { get; init; } = string.Empty;
        public string ISBN { get; init; } = string.Empty;
        public OrderCategory Category { get; init; }
        public TimeSpan ValidationDuration { get; init; }
        public TimeSpan DatabaseSaveDuration { get; init; }
        public TimeSpan TotalDuration { get; init; }
        public bool Success { get; init; }
        public string? ErrorReason { get; init; }
    }

    /// <summary>
    /// Extension methods for structured logging
    /// </summary>
    public static class LoggingExtensions
    {
        public static void LogOrderCreationMetrics(
            this ILogger logger, OrderCreationMetrics metrics)
        {
            if (logger == null || metrics == null) return;
            
            var eventId = new EventId(LogEvents.OrderCreationCompleted, "OrderCreationMetrics");
            logger.LogInformation(eventId,
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
}