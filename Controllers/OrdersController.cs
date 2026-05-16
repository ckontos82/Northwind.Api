using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Api.Data;

namespace Northwind.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly NorthwindContext _context;

        public OrdersController(NorthwindContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] List<int>? ids = null)
        {
            if (ids is not null && ids.Any(id => id <= 0))
            {
                return BadRequest("All order IDs must be positive integers.");
            }

            var query = _context.Orders.AsNoTracking();

            if (ids is { Count: > 0 })
            {
                var ordersByIds = await query
                    .Where(o => ids.Contains(o.OrderId))
                    .OrderBy(o => o.OrderId)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderDate,
                        o.RequiredDate,
                        o.ShippedDate,
                        o.CustomerId,
                        o.ShipName,
                        o.ShipCity,
                        o.ShipCountry,
                        OrderLinesCount = o.OrderDetails.Count,
                        Details = o.OrderDetails.Select(od => new
                        {
                            od.ProductId,
                            od.Product.ProductName,
                            od.UnitPrice,
                            od.Quantity,
                            od.Discount,
                            LineTotal = od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)
                        })
                    })
                    .ToListAsync();

                return Ok(new
                {
                    FilteredByIds = true,
                    RequestedIds = ids,
                    Count = ordersByIds.Count,
                    Items = ordersByIds
                });
            }

            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 50;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderBy(o => o.OrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.RequiredDate,
                    o.ShippedDate,
                    o.CustomerId,
                    o.ShipName,
                    o.ShipCity,
                    o.ShipCountry,
                    OrderLinesCount = o.OrderDetails.Count,
                    Details = o.OrderDetails.Select(od => new
                    {
                        od.ProductId,
                        od.Product.ProductName,
                        od.UnitPrice,
                        od.Quantity,
                        od.Discount,
                        LineTotal = od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = orders
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == id)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.RequiredDate,
                    o.ShippedDate,
                    o.CustomerId,
                    o.EmployeeId,
                    o.ShipName,
                    o.ShipAddress,
                    o.ShipCity,
                    o.ShipRegion,
                    o.ShipPostalCode,
                    o.ShipCountry,
                    Details = o.OrderDetails.Select(od => new
                    {
                        od.ProductId,
                        od.Product.ProductName,
                        od.UnitPrice,
                        od.Quantity,
                        od.Discount,
                        LineTotal = od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)
                    })
                })
                .FirstOrDefaultAsync();

            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }
    }
}