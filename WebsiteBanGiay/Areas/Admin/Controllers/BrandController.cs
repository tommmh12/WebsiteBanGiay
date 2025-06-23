using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanGiay.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace WebsiteBanGiay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BrandController> _logger;

        public BrandController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<BrandController> logger) : base(logger, context)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: Admin/Brand
        public async Task<IActionResult> Index()
        {
            try
            {
                var brands = await _context.Brands
                    .AsNoTracking()
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();

                return View(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách thương hiệu");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách thương hiệu";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Admin/Brand/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var brand = await _context.Brands
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.BrandId == id);

                if (brand == null)
                {
                    return NotFound();
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xem chi tiết thương hiệu ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi xem chi tiết thương hiệu";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            // Bỏ qua validate navigation property
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload ảnh
                    if (brand.LogoFile != null && brand.LogoFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "brands");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(brand.LogoFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await brand.LogoFile.CopyToAsync(stream);
                        }

                        brand.LogoUrl = "/uploads/brands/" + fileName;
                    }
                    else
                    {
                        // Gán logo mặc định nếu không upload
                        brand.LogoUrl = "/images/default-brand.png";
                    }

                    // Debug: Log trạng thái trước khi thêm
                    _logger.LogInformation($"Before Add - State: {_context.Entry(brand).State}");

                    // Thêm brand vào DbContext
                    _context.Brands.Add(brand);

                    // Debug: Log trạng thái sau khi thêm
                    _logger.LogInformation($"After Add - State: {_context.Entry(brand).State}");

                    // Lưu thay đổi và kiểm tra kết quả
                    int result = await _context.SaveChangesAsync();
                    _logger.LogInformation($"SaveChanges result: {result}, New ID: {brand.BrandId}");

                    if (result > 0)
                    {
                        TempData["Success"] = $"Đã thêm thương hiệu {brand.BrandName} thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không có dòng nào được thêm vào database");
                        _logger.LogWarning("SaveChangesAsync returned 0 affected rows");
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DbUpdateException when saving brand");
                    ModelState.AddModelError("", "Lỗi khi lưu vào database. Có thể trùng tên thương hiệu hoặc lỗi kết nối.");
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "IO Exception when uploading logo");
                    ModelState.AddModelError("LogoFile", "Lỗi khi upload logo: " + ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error when creating brand");
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            else
            {
                // Log tất cả lỗi validation
                var validationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Errors = string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))
                    });

                _logger.LogWarning($"Validation errors: {JsonSerializer.Serialize(validationErrors)}");
            }

            return View(brand);
        }
        // GET: Admin/Brand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form chỉnh sửa thương hiệu ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải form chỉnh sửa";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.BrandId)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    _logger.LogError("ModelState errors: " + JsonSerializer.Serialize(errors));

                    // Hoặc debug tại đây
                    foreach (var error in errors)
                    {
                        _logger.LogError($"Field: {error.Key}, Errors: {string.Join(", ", error.Errors)}");
                    }

                    return View(brand);
                }

                var existingBrand = await _context.Brands.FindAsync(id);
                if (existingBrand == null)
                {
                    return NotFound();
                }

                // Xử lý upload logo mới nếu có
                if (brand.LogoFile != null && brand.LogoFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(brand.LogoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("LogoFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                        return View(brand);
                    }

                    if (brand.LogoFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("LogoFile", "Kích thước file không được vượt quá 2MB");
                        return View(brand);
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "brands");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existingBrand.LogoUrl))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, existingBrand.LogoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await brand.LogoFile.CopyToAsync(fileStream);
                    }

                    existingBrand.LogoUrl = $"/uploads/brands/{uniqueFileName}";
                }

                // Cập nhật các trường khác
                existingBrand.BrandName = brand.BrandName;
                existingBrand.Description = brand.Description;

                _context.Update(existingBrand);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã cập nhật thương hiệu {brand.BrandName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!BrandExists(brand.BrandId))
                {
                    return NotFound();
                }
                _logger.LogError(ex, $"Lỗi đồng thời khi cập nhật thương hiệu ID: {id}");
                ModelState.AddModelError("", "Lỗi đồng thời khi lưu thay đổi. Vui lòng thử lại.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Lỗi database khi cập nhật thương hiệu ID: {id}");
                ModelState.AddModelError("", "Không thể cập nhật thương hiệu. Có thể tên thương hiệu đã tồn tại.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý file logo cho thương hiệu ID: {id}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý logo. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi cập nhật thương hiệu ID: {id}");
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            return View(brand);
        }

        // GET: Admin/Brand/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var brand = await _context.Brands
                    .FirstOrDefaultAsync(m => m.BrandId == id);

                if (brand == null)
                {
                    return NotFound();
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form xóa thương hiệu ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải form xóa";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brand/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                // Xóa ảnh logo nếu tồn tại
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, brand.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa thương hiệu {brand.BrandName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Lỗi database khi xóa thương hiệu ID: {id}");
                TempData["Error"] = "Không thể xóa thương hiệu do có sản phẩm liên quan";
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa file logo của thương hiệu ID: {id}");
                TempData["Error"] = "Đã xóa thương hiệu nhưng có lỗi khi xóa logo";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi xóa thương hiệu ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi hệ thống khi xóa thương hiệu";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }
    }
}