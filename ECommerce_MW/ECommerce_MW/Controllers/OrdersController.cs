using ECommerce_MW.DAL;
using ECommerce_MW.DAL.Entities;
using ECommerce_MW.Enums;
using ECommerce_MW.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vereyon.Web;

namespace ECommerce_MW.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly IFlashMessage _flashMessage;
        private readonly IOrderHelper _orderHelper;

        public OrdersController(DatabaseContext context , IFlashMessage flashMessage, IOrderHelper orderHelper)
        {
            _context = context;
            _flashMessage = flashMessage;
            _orderHelper = orderHelper;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Orders
                .Include(s => s.User)
                .Include(s => s.OrderDetails)
                .ThenInclude(sd => sd.Product)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(Guid? orderId)
        {
            if (orderId == null) return NotFound();

            Order order = await _context.Orders
                .Include(s => s.User)
                .Include(s => s.OrderDetails)
                .ThenInclude(sd => sd.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(s => s.Id == orderId);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> DispatchOrder(Guid? orderId)
        {
            if (orderId == null) return NotFound();

            Order order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.OrderStatus != OrderStatus.Nuevo)
            _flashMessage.Danger(String.Format("Solo se pueden enviar pedidos que estén en estado '{0}'.", OrderStatus.Nuevo));

            else
            {
                order.OrderStatus = OrderStatus.Despachado;
                order.ModifiedDate = DateTime.Now;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _flashMessage.Confirmation(String.Format("El estado del pedido ha sido cambiado a '{0}'.", OrderStatus.Despachado));

            }

            return RedirectToAction(nameof(Details), new { orderId = order.Id });
        }

        public async Task<IActionResult> SendOrder(Guid? orderId)
        {
            if (orderId == null) return NotFound();

            Order order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.OrderStatus != OrderStatus.Despachado)
                _flashMessage.Danger(String.Format("Solo se pueden enviar pedidos que estén en estado '{0}'.", OrderStatus.Despachado));
            else
            {
                order.OrderStatus = OrderStatus.Enviado;
                order.ModifiedDate = DateTime.Now;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _flashMessage.Confirmation(String.Format("El estado del pedido ha sido cambiado a '{0}'.", OrderStatus.Enviado));
            }

            return RedirectToAction(nameof(Details), new { orderId = order.Id });
        }

        public async Task<IActionResult> ConfirmOrder(Guid? orderId)
        {
            if (orderId == null) return NotFound();

            Order order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.OrderStatus != OrderStatus.Enviado)
                _flashMessage.Danger(String.Format("Solo se pueden enviar pedidos que estén en estado '{0}'.", OrderStatus.Enviado));
            else
            {
                order.OrderStatus = OrderStatus.Confirmado;
                order.ModifiedDate = DateTime.Now;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _flashMessage.Confirmation(String.Format("El estado del pedido ha sido cambiado a '{0}'.", OrderStatus.Confirmado));

            }

            return RedirectToAction(nameof(Details), new { orderId = order.Id });
        }

        public async Task<IActionResult> CancelOrder(Guid? orderId)
        {
            if (orderId == null) return NotFound();

            Order order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.OrderStatus == OrderStatus.Cancelado)
                _flashMessage.Danger(String.Format("No se puede cancelar un pedido que esté en estado '{0}'.", OrderStatus.Cancelado));
            else
            {
                await _orderHelper.CancelOrderAsync(order.Id);
                _flashMessage.Confirmation(String.Format("El estado del pedido ha sido cambiado a '{0}'.", OrderStatus.Cancelado));
            }

            return RedirectToAction(nameof(Details), new { orderId = order.Id });
        }
    }
}
