using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Services;

namespace OrderManagementAPI.Controllers
{
    //controller for order management with validation and logging
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        //create a new order
        /// <param name="request">order creation request</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Created order profile</returns>
        [HttpPost]
        public async Task<ActionResult<OrderProfileDto>> CreateOrder(
            [FromBody] CreateOrderProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _orderService.CreateOrderAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { 
                    Error = "Validation failed", 
                    Details = ex.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage })
                });
            }
            catch (Exception ex)
            {
                return Problem(title: "An error occurred while creating the order", detail: ex.Message);
            }
        }

        //get all orders (for testing and demonstration)
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>List of all orders</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllOrders(CancellationToken cancellationToken = default)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync(cancellationToken);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return Problem(title: "An error occurred while retrieving orders", detail: ex.Message);
            }
        }

        //get order by ID (placeholder for future implementation)
        /// <param name="id">Order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderProfileDto>> GetOrderById(Guid id)
        {
            // this would be implemented with actual repository method
            await Task.CompletedTask; // placeholder to make method truly async
            return NotFound(new { Message = "Order retrieval by ID not yet implemented" });
        }
    }
}