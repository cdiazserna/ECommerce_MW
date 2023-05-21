using ECommerce_MW.Common;
using ECommerce_MW.DAL;
using ECommerce_MW.DAL.Entities;
using ECommerce_MW.Enums;
using ECommerce_MW.Helpers;
using ECommerce_MW.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce_MW.Services
{
    public class OrderHelper : IOrderHelper
    {
        private readonly DatabaseContext _context;

        public OrderHelper(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Response> ProcessOrderAsync(ShowCartViewModel showCartViewModel)
        {
            Response response = await CheckInventoryAsync(showCartViewModel);
            if (!response.IsSuccess) return response;

            Order order = new()
            {
                CreatedDate = DateTime.Now,
                User = showCartViewModel.User,
                Remarks = showCartViewModel.Remarks,
                OrderDetails = new List<OrderDetail>(),
                OrderStatus = OrderStatus.Nuevo
            };

            foreach (TemporalSale? item in showCartViewModel.TemporalSales)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    Product = item.Product,
                    Quantity = item.Quantity,
                    Remarks = item.Remarks,
                });

                Product product = await _context.Products.FindAsync(item.Product.Id);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    _context.Products.Update(product);
                }

                _context.TemporalSales.Remove(item);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return response;
        }

        private async Task<Response> CheckInventoryAsync(ShowCartViewModel showCartViewModel)
        {
            Response response = new()
            { 
                IsSuccess = true 
            };

            foreach (TemporalSale item in showCartViewModel.TemporalSales)
            {
                Product product = await _context.Products.FindAsync(item.Product.Id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = $"El producto {item.Product.Name}, ya no está disponible";
                    return response;
                }

                if (product.Stock < item.Quantity)
                {
                    response.IsSuccess = false;
                    response.Message = $"Lo sentimos, solo tenemos {item.Quantity} unidades del producto {item.Product.Name}, para tomar su pedido. Dismuya la cantidad.";
                    return response;
                }
            }
            return response;
        }

        public async Task<Response> CancelOrderAsync(Guid orderId)
        {
            Order order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            foreach (OrderDetail orderDetail in order.OrderDetails)
            {
                Product product = await _context.Products.FindAsync(orderDetail.Product.Id);
                if (product != null)
                    product.Stock += orderDetail.Quantity;
            }

            order.OrderStatus = OrderStatus.Cancelado;
            await _context.SaveChangesAsync();
            return new Response { IsSuccess = true };
        }
    }
}
