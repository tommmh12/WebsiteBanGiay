using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanGiay.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace WebsiteBanGiay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ApplicationDbContext context,
            ILogger<CategoriesController> logger) : base(logger, context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách danh mục");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách danh mục";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,Description")] Category category)
        {
            // Bỏ qua validate navigation property
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã thêm danh mục {category.CategoryName} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DbUpdateException khi thêm danh mục");
                    ModelState.AddModelError("", "Lỗi khi lưu vào database. Có thể trùng tên danh mục hoặc lỗi kết nối.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không xác định khi thêm danh mục");
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            else
            {
                // Log lỗi validation
                var validationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Errors = string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))
                    });

                _logger.LogWarning($"Lỗi validation khi tạo danh mục: {string.Join("; ", validationErrors)}");
            }

            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form chỉnh sửa danh mục ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải form chỉnh sửa";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,Description")] Category category)
        {
            if (id != category.CategoryId)
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

                    _logger.LogError("Lỗi ModelState: " + string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(",", e.Errors)}")));
                    return View(category);
                }

                var existingCategory = await _context.Categories.FindAsync(id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                existingCategory.CategoryName = category.CategoryName;
                existingCategory.Description = category.Description;

                _context.Update(existingCategory);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã cập nhật danh mục {category.CategoryName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CategoryExists(category.CategoryId))
                {
                    return NotFound();
                }
                _logger.LogError(ex, $"Lỗi đồng thời khi cập nhật danh mục ID: {id}");
                ModelState.AddModelError("", "Lỗi đồng thời khi lưu thay đổi. Vui lòng thử lại.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Lỗi database khi cập nhật danh mục ID: {id}");
                ModelState.AddModelError("", "Không thể cập nhật danh mục. Có thể tên danh mục đã tồn tại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi cập nhật danh mục ID: {id}");
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(m => m.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải form xóa danh mục ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi tải form xóa";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa danh mục {category.CategoryName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Lỗi database khi xóa danh mục ID: {id}");
                TempData["Error"] = "Không thể xóa danh mục do có sản phẩm liên quan";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi xóa danh mục ID: {id}");
                TempData["Error"] = "Đã xảy ra lỗi hệ thống khi xóa danh mục";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}