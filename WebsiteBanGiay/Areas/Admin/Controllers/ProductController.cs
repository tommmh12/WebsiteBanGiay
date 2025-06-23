using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanGiay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebsiteBanGiay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : AdminBaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<ProductController> logger) : base(logger, context)
        {
            _env = env;
            _logger = logger;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName);

                var (products, totalItems) = ApplyPagination(query, page, pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách sản phẩm");
                return AdminErrorView("Đã xảy ra lỗi khi tải danh sách sản phẩm", ex);
            }
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return HandleNotFound("Không tìm thấy ID sản phẩm");
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ProductId == id);

                if (product == null)
                {
                    return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xem chi tiết sản phẩm ID: {id}");
                return AdminErrorView($"Đã xảy ra lỗi khi xem chi tiết sản phẩm ID: {id}", ex);
            }
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Bỏ qua validate các navigation properties
            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("Images");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("CartItems");
            ModelState.Remove("Reviews");

            // Tự động tạo slug nếu để trống
            if (string.IsNullOrEmpty(product.Slug))
            {
                product.Slug = GenerateSlug(product.ProductName);
                ModelState.Remove("Slug"); // Xóa validation error nếu có
            }
            if (ModelState.IsValid)
            {
                try
                {
                    if (product.ImageFile != null && product.ImageFile.Length > 0)
                    {
                        product.ImageUrl = await SaveFile(product.ImageFile, "products");
                        ModelState.Remove("ImageUrl"); // Xóa validation error
                    }
                    else if (string.IsNullOrEmpty(product.ImageUrl))
                    {
                        ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh đại diện");
                        await LoadDropdownData();
                        return View(product);
                    }

                    // Xử lý nhiều ảnh sản phẩm
                    if (product.ProductImages != null && product.ProductImages.Count > 0)
                    {
                        product.Images = new List<ProductImage>();
                        foreach (var imageFile in product.ProductImages)
                        {
                            if (imageFile.Length > 0)
                            {
                                var imageUrl = await SaveFile(imageFile, "products/images");
                                product.Images.Add(new ProductImage
                                {
                                    Url = imageUrl,
                                    IsMain = false,
                                    SortOrder = 0
                                });
                            }
                        }
                    }

                    // Tạo slug nếu chưa có
                    if (string.IsNullOrEmpty(product.Slug))
                    {
                        product.Slug = GenerateSlug(product.ProductName);
                    }

                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;

                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã thêm sản phẩm {product.ProductName} thành công!";
                    return RedirectToAction(nameof(Index));
                }

                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Lỗi database khi thêm sản phẩm");
                    ModelState.AddModelError("", "Lỗi khi lưu sản phẩm. Có thể tên sản phẩm đã tồn tại.");
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý ảnh sản phẩm");
                    ModelState.AddModelError("", "Lỗi khi lưu ảnh sản phẩm. Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không xác định khi thêm sản phẩm");
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
                }

            }

            await LoadDropdownData();
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return HandleNotFound("Không tìm thấy ID sản phẩm");
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                }

                await LoadDropdownData();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form chỉnh sửa sản phẩm ID: {id}");
                return AdminErrorView($"Đã xảy ra lỗi khi tải form chỉnh sửa sản phẩm ID: {id}", ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return HandleNotFound("ID sản phẩm không khớp");
            }

            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("Images");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("CartItems");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.ProductId == id);

                    if (existingProduct == null)
                    {
                        return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                    }

                    // Cập nhật thông tin cơ bản
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Slug = string.IsNullOrEmpty(product.Slug) ? GenerateSlug(product.ProductName) : product.Slug;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.DiscountPrice = product.DiscountPrice;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.BrandId = product.BrandId;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    // Xử lý ảnh đại diện nếu có thay đổi
                    if (product.ImageFile != null && product.ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                        {
                            DeleteFile(existingProduct.ImageUrl);
                        }
                        existingProduct.ImageUrl = await SaveFile(product.ImageFile, "products");
                    }

                    // Xử lý thêm ảnh sản phẩm nếu có
                    if (product.ProductImages != null && product.ProductImages.Count > 0)
                    {
                        foreach (var imageFile in product.ProductImages)
                        {
                            if (imageFile.Length > 0)
                            {
                                var imageUrl = await SaveFile(imageFile, "products/images");
                                existingProduct.Images.Add(new ProductImage
                                {
                                    Url = imageUrl,
                                    IsMain = false,
                                    SortOrder = 0
                                });
                            }
                        }
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã cập nhật sản phẩm {product.ProductName} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                    }
                    _logger.LogError(ex, $"Lỗi đồng thời khi cập nhật sản phẩm ID: {id}");
                    ModelState.AddModelError("", "Lỗi đồng thời khi lưu thay đổi. Vui lòng thử lại.");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, $"Lỗi database khi cập nhật sản phẩm ID: {id}");
                    ModelState.AddModelError("", "Không thể cập nhật sản phẩm. Có thể tên sản phẩm đã tồn tại.");
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, $"Lỗi khi xử lý ảnh sản phẩm ID: {id}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý ảnh. Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi không xác định khi cập nhật sản phẩm ID: {id}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
                }
            }

            await LoadDropdownData();
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return HandleNotFound("Không tìm thấy ID sản phẩm");
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(m => m.ProductId == id);

                if (product == null)
                {
                    return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form xóa sản phẩm ID: {id}");
                return AdminErrorView($"Đã xảy ra lỗi khi tải form xóa sản phẩm ID: {id}", ex);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return HandleNotFound($"Không tìm thấy sản phẩm với ID: {id}");
                }

                // Xóa ảnh đại diện
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteFile(product.ImageUrl);
                }

                // Xóa các ảnh sản phẩm
                foreach (var image in product.Images)
                {
                    DeleteFile(image.Url);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa sản phẩm {product.ProductName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Lỗi database khi xóa sản phẩm ID: {id}");
                TempData["Error"] = "Không thể xóa sản phẩm do có đơn hàng liên quan";
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa ảnh sản phẩm ID: {id}");
                TempData["Error"] = "Đã xóa sản phẩm nhưng có lỗi khi xóa ảnh";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi xóa sản phẩm ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi hệ thống khi xóa sản phẩm";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            try
            {
                var image = await _context.ProductImages.FindAsync(imageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh" });
                }

                DeleteFile(image.Url);
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa ảnh thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa ảnh ID: {imageId}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa ảnh" });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        #region Helper Methods
        private async Task LoadDropdownData()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.BrandName)
                .ToListAsync();
        }

        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        private void DeleteFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        private string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "");
        }
        #endregion
    }
}