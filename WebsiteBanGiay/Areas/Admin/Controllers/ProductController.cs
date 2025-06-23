using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteBanGiay.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace WebsiteBanGiay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : AdminBaseController
    {
        private const int MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<ProductController> logger) : base(logger, context)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product list");
                TempData["Error"] = "An error occurred while loading the product list";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing product details ID: {id}");
                TempData["Error"] = "An error occurred while viewing product details";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await PrepareSelectLists();
                return View(new Product());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product creation form");
                TempData["Error"] = "An error occurred while loading the product creation form";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile mainImage, List<IFormFile> additionalImages)
        {
            // Ignore navigation property validation
            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("Images");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("CartItems");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid && mainImage != null && mainImage.Length > 0)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate main image
                    var fileExtension = Path.GetExtension(mainImage.FileName).ToLower();
                    if (!AllowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("mainImage", "Chỉ cho phép file jpg, jpeg, png, gif");
                        await PrepareSelectLists();
                        return View(product);
                    }

                    if (mainImage.Length > MaxFileSize)
                    {
                        ModelState.AddModelError("mainImage", "Kích thước file không vượt quá 5MB");
                        await PrepareSelectLists();
                        return View(product);
                    }

                    // Process main image
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
                    Directory.CreateDirectory(uploadsFolder);
                    string fileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mainImage.CopyToAsync(stream);
                    }

                    product.ImageUrl = "/uploads/products/" + fileName;
                    product.CreatedAt = DateTime.UtcNow;

                    _context.Products.Add(product);
                    int result = await _context.SaveChangesAsync();

                    if (result == 0)
                    {
                        ModelState.AddModelError("", "Không thể thêm sản phẩm vào cơ sở dữ liệu");
                        await PrepareSelectLists();
                        return View(product);
                    }

                    // Process additional images
                    if (additionalImages != null && additionalImages.Any())
                    {
                        string galleryFolder = Path.Combine(_env.WebRootPath, "uploads", "products", "gallery");
                        Directory.CreateDirectory(galleryFolder);

                        foreach (var img in additionalImages)
                        {
                            fileExtension = Path.GetExtension(img.FileName).ToLower();
                            if (!AllowedExtensions.Contains(fileExtension) || img.Length > MaxFileSize)
                            {
                                _logger.LogWarning($"Bỏ qua ảnh bổ sung không hợp lệ: {img.FileName}");
                                continue;
                            }

                            fileName = Guid.NewGuid().ToString() + fileExtension;
                            filePath = Path.Combine(galleryFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await img.CopyToAsync(stream);
                            }

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.ProductId,
                                Url = "/uploads/products/gallery/" + fileName
                            });
                        }

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["Success"] = $"Sản phẩm {product.ProductName} được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi tạo sản phẩm");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo PLEASE ASSISTANCE FROM MY AI tạo sản phẩm");
                }
            }
            else
            {
                if (mainImage == null || mainImage.Length == 0)
                {
                    ModelState.AddModelError("mainImage", "Ảnh đại diện là bắt buộc");
                }

                var validationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))
                    });

                _logger.LogWarning($"Validation errors: {JsonSerializer.Serialize(validationErrors)}");
            }

            await PrepareSelectLists();
            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound();
                }

                await PrepareSelectLists(product.CategoryId, product.BrandId);
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit form for product ID: {id}");
                TempData["Error"] = "An error occurred while loading the edit form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile newMainImage, List<IFormFile> newAdditionalImages, List<int> deleteImages)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            // Ignore navigation property validation
            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("Images");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("CartItems");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.ProductId == id);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Process new main image
                    if (newMainImage != null && newMainImage.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(newMainImage.FileName).ToLower();
                        if (!AllowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("newMainImage", "Only image files (jpg, jpeg, png, gif) are allowed");
                            await PrepareSelectLists(product.CategoryId, product.BrandId);
                            return View(product);
                        }

                        if (newMainImage.Length > MaxFileSize)
                        {
                            ModelState.AddModelError("newMainImage", "File size cannot exceed 5MB");
                            await PrepareSelectLists(product.CategoryId, product.BrandId);
                            return View(product);
                        }

                        // Delete old main image
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                        {
                            var oldFilePath = Path.Combine(_env.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
                        string fileName = Guid.NewGuid().ToString() + fileExtension;
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await newMainImage.CopyToAsync(stream);
                        }

                        existingProduct.ImageUrl = "/uploads/products/" + fileName;
                    }

                    // Update product properties
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.BrandId = product.BrandId;
                    existingProduct.IsActive = product.IsActive;

                    // Process images to delete
                    if (deleteImages != null && deleteImages.Any())
                    {
                        var imagesToDelete = existingProduct.Images
                            .Where(img => deleteImages.Contains(img.ImageId))
                            .ToList();

                        foreach (var img in imagesToDelete)
                        {
                            var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            _context.ProductImages.Remove(img);
                        }
                    }

                    // Process new additional images
                    if (newAdditionalImages != null && newAdditionalImages.Any())
                    {
                        string galleryFolder = Path.Combine(_env.WebRootPath, "uploads", "products", "gallery");
                        if (!Directory.Exists(galleryFolder))
                        {
                            Directory.CreateDirectory(galleryFolder);
                        }

                        foreach (var img in newAdditionalImages)
                        {
                            var fileExtension = Path.GetExtension(img.FileName).ToLower();
                            if (!AllowedExtensions.Contains(fileExtension) || img.Length > MaxFileSize)
                            {
                                _logger.LogWarning($"Skipping invalid additional image: {img.FileName}");
                                continue;
                            }

                            string fileName = Guid.NewGuid().ToString() + fileExtension;
                            string filePath = Path.Combine(galleryFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await img.CopyToAsync(stream);
                            }

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.ProductId,
                                Url = "/uploads/products/gallery/" + fileName
                            });
                        }
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"Product {product.ProductName} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error updating product ID: {id}");
                    ModelState.AddModelError("", "An error occurred while updating the product");
                }
            }
            else
            {
                var validationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))
                    });

                _logger.LogWarning($"Validation errors: {JsonSerializer.Serialize(validationErrors)}");
            }

            await PrepareSelectLists(product.CategoryId, product.BrandId);
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading delete form for product ID: {id}");
                TempData["Error"] = "An error occurred while loading the delete form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound();
                }

                // Delete main image
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Delete additional images
                foreach (var img in product.Images)
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Product {product.ProductName} deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting product ID: {id}");
                TempData["Error"] = "An error occurred while deleting the product";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        private async Task PrepareSelectLists(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            try
            {
                ViewBag.Categories = new SelectList(
                    await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(),
                    "CategoryId", "CategoryName", selectedCategoryId);

                ViewBag.Brands = new SelectList(
                    await _context.Brands.AsNoTracking().OrderBy(b => b.BrandName).ToListAsync(),
                    "BrandId", "BrandName", selectedBrandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories and brands");
                TempData["Error"] = "Error loading categories and brands";
            }
        }

        #endregion
    }
}