using ECommerce_MW.Common;
using ECommerce_MW.DAL;
using ECommerce_MW.DAL.Entities;
using ECommerce_MW.Helpers;
using ECommerce_MW.Models;
using ECommerce_MW.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ECommerce_MW.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly IUserHelper _userHelper;
        private readonly IOrderHelper _orderHelper;

        public HomeController(DatabaseContext context, IUserHelper userHelper, IOrderHelper orderHelper)
        {
            _context = context;
            _userHelper = userHelper;
            _orderHelper = orderHelper;
        }

        public async Task<IActionResult> Index()
        {
            List<Product>? products = await _context.Products
               .Include(p => p.ProductImages)
               .Include(p => p.ProductCategories)
               .OrderBy(p => p.Description)
               .ToListAsync();

            ViewBag.UserFullName = GetUserFullName();

            //Begins New change
            HomeViewModel homeViewModel = new() { Products = products };

            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user != null)
            {
                homeViewModel.Quantity = await _context.TemporalSales
                    .Where(ts => ts.User.Id == user.Id)
                    .SumAsync(ts => ts.Quantity);
            }

            return View(homeViewModel);
            //Ends New change
        }

        private string GetUserFullName()
        {
            return _context.Users
                 .Where(u => u.Email == User.Identity.Name)
                 .Select(u => u.FullName)
                 .FirstOrDefault();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

		[Route("error/404")]
		public IActionResult Error404()
        {
            return View();
        }

        public async Task<IActionResult> AddProductInKart(Guid? productId)
        {
            if (productId == null) return NotFound();

            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            Product product = await _context.Products.FindAsync(productId);
            User user = await _userHelper.GetUserAsync(User.Identity.Name);

            if (user == null || product == null) return NotFound();

            // Busca una entrada existente en la tabla TemporalSale para este producto y usuario
            TemporalSale existingTemporalSale = await _context.TemporalSales
                .Where(t => t.Product.Id == productId && t.User.Id == user.Id)
                .FirstOrDefaultAsync();

            if (existingTemporalSale != null)
            {
                // Si existe una entrada, incrementa la cantidad
                existingTemporalSale.Quantity += 1;
                existingTemporalSale.ModifiedDate = DateTime.Now;
            }
            else
            {
                // Si no existe una entrada, crea una nueva
                TemporalSale temporalSale = new()
                {
                    CreatedDate = DateTime.Now,
                    Product = product,
                    Quantity = 1,
                    User = user
                };

                _context.TemporalSales.Add(temporalSale);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DetailsProduct(Guid? productId)
        {
            if (productId == null) return NotFound();

            Product product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            string categories = string.Empty;

            foreach (ProductCategory? category in product.ProductCategories)
                categories += $"{category.Category.Name}, ";

            categories = categories.Substring(0, categories.Length - 2);

            DetailsProductToCartViewModel detailsProductToCartViewModel = new()
            {
                Categories = categories,
                Description = product.Description,
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                ProductImages = product.ProductImages,
                Quantity = 1,
                Stock = product.Stock,
            };

            return View(detailsProductToCartViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetailsProduct(DetailsProductToCartViewModel detailsProductToCartViewModel)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            Product product = await _context.Products.FindAsync(detailsProductToCartViewModel.Id);
            User user = await _userHelper.GetUserAsync(User.Identity.Name);

            if (product == null || user == null) return NotFound();

            // Busca una entrada existente en la tabla TemporalSale para este producto y usuario
            TemporalSale existingTemporalSale = await _context.TemporalSales
                .Where(t => t.Product.Id == detailsProductToCartViewModel.Id && t.User.Id == user.Id)
                .FirstOrDefaultAsync();

            if (existingTemporalSale != null)
            {                
                // Si existe una entrada, incrementa la cantidad
                existingTemporalSale.Quantity += detailsProductToCartViewModel.Quantity;
                existingTemporalSale.ModifiedDate = DateTime.Now;
            }
            else
            {
                // Si no existe una entrada, crea una nueva
                TemporalSale temporalSale = new()
                {
                    CreatedDate = DateTime.Now,
                    Product = product,
                    Quantity = 1,
                    User = user,
                    Remarks = detailsProductToCartViewModel.Remarks,
                };

                _context.TemporalSales.Add(temporalSale);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize] //Etiqueta para que solo usuarios logueados puedan acceder a este método.
        public async Task<IActionResult> ShowCartAndConfirm()
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null) return NotFound();

            List<TemporalSale>? temporalSales = await _context.TemporalSales
                .Include(ts => ts.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(ts => ts.User.Id == user.Id)
                .ToListAsync();

            ShowCartViewModel showCartViewModel = new()
            {
                User = user,
                TemporalSales = temporalSales,
            };

            return View(showCartViewModel);
        }

        public async Task<IActionResult> DecreaseQuantity(Guid? temporalSaleId)
        {
            if (temporalSaleId == null) return NotFound();

            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(temporalSaleId);
            if (temporalSale == null) return NotFound();

            if (temporalSale.Quantity > 1)
            {
                temporalSale.ModifiedDate = DateTime.Now;
                temporalSale.Quantity--;
                _context.TemporalSales.Update(temporalSale);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ShowCartAndConfirm));
        }

        public async Task<IActionResult> IncreaseQuantity(Guid? temporalSaleId)
        {
            if (temporalSaleId == null) return NotFound();

            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(temporalSaleId);
            if (temporalSale == null) return NotFound();

            temporalSale.ModifiedDate = DateTime.Now;
            temporalSale.Quantity++;
            _context.TemporalSales.Update(temporalSale);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ShowCartAndConfirm));
        }

        public async Task<IActionResult> DeleteTemporalSale(Guid? temporalSaleId)
        {
            if (temporalSaleId == null) return NotFound();

            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(temporalSaleId);
            if (temporalSale == null) return NotFound();

            _context.TemporalSales.Remove(temporalSale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowCartAndConfirm));
        }

        public async Task<IActionResult> EditTemporalSale(Guid? temporalSaleId)
        {
            if (temporalSaleId == null) return NotFound();

            TemporalSale temporalSale = await _context.TemporalSales.FindAsync(temporalSaleId);
            if (temporalSale == null) return NotFound();

            EditTemporalSaleViewModel editTemporalSaleViewModel = new()
            {
                Id = temporalSale.Id,
                Quantity = temporalSale.Quantity,
                Remarks = temporalSale.Remarks,
            };

            return View(editTemporalSaleViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTemporalSale(Guid? temporalSaleId, EditTemporalSaleViewModel editTemporalSaleViewModel)
        {
            if (temporalSaleId != editTemporalSaleViewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    TemporalSale temporalSale = await _context.TemporalSales.FindAsync(temporalSaleId);
                    temporalSale.Quantity = editTemporalSaleViewModel.Quantity;
                    temporalSale.Remarks = editTemporalSaleViewModel.Remarks;
                    temporalSale.ModifiedDate = DateTime.Now;
                    _context.Update(temporalSale);
                    await _context.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                    return View(editTemporalSaleViewModel);
                }

                return RedirectToAction(nameof(ShowCartAndConfirm));
            }

            return View(editTemporalSaleViewModel);
        }

        [Authorize]
        public IActionResult OrderSuccess()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowCartAndConfirm(ShowCartViewModel showCartViewModel)
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null) return NotFound();

            showCartViewModel.User = user;
            showCartViewModel.TemporalSales = await _context.TemporalSales
                .Include(ts => ts.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(ts => ts.User.Id == user.Id)
            .ToListAsync();

            Response response = await _orderHelper.ProcessOrderAsync(showCartViewModel);
            if (response.IsSuccess) return RedirectToAction(nameof(OrderSuccess));

            ModelState.AddModelError(string.Empty, response.Message);
            return View(showCartViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null) return NotFound();

            List<TemporalSale> temporalSale = await _context.TemporalSales
                .Where(ts => ts.User.Id == user.Id)
                .ToListAsync();

            _context.RemoveRange(temporalSale);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}