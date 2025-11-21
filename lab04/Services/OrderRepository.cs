using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Data;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Services
{
    //repository interface for Order operations
    public interface IOrderRepository
    {
        Task<Order?> GetByISBNAsync(string isbn, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTitleAndAuthorAsync(string title, string author, CancellationToken cancellationToken = default);
        Task<int> GetDailyOrderCountAsync(DateTime date, CancellationToken cancellationToken = default);
        Task AddAsync(Order order, CancellationToken cancellationToken = default);
        Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    //repository implementation for Order operations
    public class OrderRepository : IOrderRepository
    {
        private readonly IApplicationContext _context;

        public OrderRepository(IApplicationContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByISBNAsync(string isbn, CancellationToken cancellationToken = default)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.ISBN == isbn, cancellationToken);
        }

        public async Task<bool> ExistsByTitleAndAuthorAsync(string title, string author, CancellationToken cancellationToken = default)
        {
            return await _context.Orders.AnyAsync(o => o.Title.ToLower() == title.ToLower() && o.Author.ToLower() == author.ToLower(), cancellationToken);
        }

        public async Task<int> GetDailyOrderCountAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            return await _context.Orders.CountAsync(o => o.CreatedAt.Date == date.Date, cancellationToken);
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders.ToListAsync(cancellationToken);
        }
    }
}