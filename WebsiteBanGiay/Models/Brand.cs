using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace WebsiteBanGiay.Models
{
    public class Brand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Tên thương hiệu là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên thương hiệu phải từ {2} đến {1} ký tự")]
        [Display(Name = "Tên Thương Hiệu")]
        public string BrandName { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500, ErrorMessage = "Mô tả không vượt quá {1} ký tự")]
        [Display(Name = "Mô Tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Logo là bắt buộc")]
        [Display(Name = "Đường Dẫn Logo")]
        public string LogoUrl { get; set; } = "/images/default-brand.png"; // Giá trị mặc định

        [NotMapped]
        [Display(Name = "Logo Thương Hiệu")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new string[] { ".jpg", ".png", ".jpeg", ".gif" }, ErrorMessage = "Chỉ chấp nhận file ảnh (jpg, png, jpeg, gif)")]
        [MaxFileSize(2 * 1024 * 1024, ErrorMessage = "Kích thước file không được vượt quá 2MB")]
        public IFormFile LogoFile { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}