using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using WebsiteBanGiay.Models;

namespace WebsiteBanGiay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken] // Tự động validate antiforgery token
    public abstract class AdminBaseController : Controller
    {
        protected readonly ILogger<AdminBaseController> _logger;
        protected readonly ApplicationDbContext _context;

        public AdminBaseController(
            ILogger<AdminBaseController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;   
            _context = context;
        }

        // Ghi log các hành động quan trọng
        protected void LogAction(string action, string message)
        {
            _logger.LogInformation($"[ADMIN ACTION] {action}: {message} - User: {User.Identity.Name}");
        }

        // Xử lý lỗi tập trung
        protected IActionResult AdminErrorView(string errorMessage, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, $"Admin Error: {errorMessage}");
            }

            ViewBag.ErrorMessage = errorMessage;
            return View("~/Areas/Admin/Views/Shared/Error.cshtml");
        }

        // Kiểm tra và xử lý trường hợp không tìm thấy
        protected IActionResult HandleNotFound(string message)
        {
            return AdminErrorView(message);
        }

        // Phương thức chung cho phân trang
        protected (IEnumerable<T>, int) ApplyPagination<T>(IQueryable<T> query, int page = 1, int pageSize = 10)
        {
            var totalItems = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, totalItems);
        }
    }
}