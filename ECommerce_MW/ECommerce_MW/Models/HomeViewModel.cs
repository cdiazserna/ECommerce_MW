using ECommerce_MW.DAL.Entities;

namespace ECommerce_MW.Models
{
    public class HomeViewModel
    {
        public ICollection<Product> Products { get; set; }

        public ICollection<Category> Categories { get; set; }

        //Esta propiedad me muestra cuánto productos llevo agregados al carrito de compras.
        public float Quantity { get; set; }
    }
}
