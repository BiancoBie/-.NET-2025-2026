using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.Data;
using OrderManagementAPI.Services;
using OrderManagementAPI.Mappers;
using OrderManagementAPI.Validators;
using OrderManagementAPI.Models;
using OrderManagementAPI.Middleware;
using OrderManagementAPI.DTOs;

var builder = WebApplication.CreateBuilder(args);

//MVC CONFIGURATION

//add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//database configuration
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseInMemoryDatabase("OrderManagementDb"));

//automapper configuration
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// fluentvalidation configuration
builder.Services.AddScoped<CreateOrderProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderProfileValidator>();

// repository and service registration - following dependency injection pattern
builder.Services.AddScoped<IApplicationContext>(provider => 
    provider.GetRequiredService<ApplicationContext>());
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<OrderService>();

// memory cache
builder.Services.AddMemoryCache();

// logging configuration
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// add correlation middleware to pipeline
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

// map controllers - this is the MVC way
app.MapControllers();

// initialize database with sample data (optional)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    await context.Database.EnsureCreatedAsync();
    
    // add some sample data for testing
    if (!await context.Orders.AnyAsync())
    {
        var sampleOrders = new[]
        {
            new Order("The Great Gatsby", "F. Scott Fitzgerald", "9780743273565", 
                OrderCategory.Fiction, 14.99m, new DateTime(1925, 4, 10), 
                "https://example.com/gatsby.jpg", 25),
            new Order("Clean Code", "Robert C. Martin", "9780132350884", 
                OrderCategory.Technical, 45.99m, new DateTime(2008, 8, 1), 
                "https://example.com/cleancode.jpg", 8),
            new Order("Where the Wild Things Are", "Maurice Sendak", "9780060254926", 
                OrderCategory.Children, 18.99m, new DateTime(1963, 5, 9), 
                null, 12) // CoverImageUrl will be null due to conditional mapping
        };

        context.Orders.AddRange(sampleOrders);
        await context.SaveChangesAsync();
    }
}

app.Run();

//program class for integration testing
public partial class Program { }