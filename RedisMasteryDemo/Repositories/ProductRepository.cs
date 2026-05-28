using RedisMasteryDemo.Interfaces;
using RedisMasteryDemo.Models;

namespace RedisMasteryDemo.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();

        public ProductRepository()
        {
            // Seed data for testing
            _products.AddRange(new List<Product>
        {
            new Product { Id = 1, Name = "Dell Laptop", Price = 45000000, Stock = 10 },
            new Product { Id = 2, Name = "Samsung Mobile", Price = 25000000, Stock = 25 },
            new Product { Id = 3, Name = "Sony Headphones", Price = 3500000, Stock = 50 }
        });
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            await Task.Delay(300); // Simulate database latency
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            await Task.Delay(400);
            return _products.ToList();
        }

        public async Task AddAsync(Product product)
        {
            product.Id = _products.Max(p => p.Id) + 1;
            _products.Add(product);
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Product product)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing != null)
            {
                _products.Remove(existing);
                _products.Add(product);
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null) _products.Remove(product);
            await Task.CompletedTask;
        }
    }
}
