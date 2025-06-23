using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanGiay.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên sản phẩm không vượt quá 150 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; }

        [Display(Name = "Đường dẫn SEO")]
        public string Slug { get; set; }

        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "Mô tả không vượt quá 1000 ký tự")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá gốc")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá khuyến mãi")]
        public decimal? DiscountPrice { get; set; }

        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; } = 0;

        [Required(ErrorMessage = "Ảnh đại diện là bắt buộc")]
        [Display(Name = "Ảnh đại diện")]
        public string ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Ảnh đại diện")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new string[] { ".jpg", ".png", ".jpeg", ".gif" }, ErrorMessage = "Chỉ chấp nhận file ảnh (jpg, png, jpeg, gif)")]
        [MaxFileSize(2 * 1024 * 1024, ErrorMessage = "Kích thước file không được vượt quá 2MB")]
        public IFormFile ImageFile { get; set; }

        [NotMapped]
        [Display(Name = "Ảnh sản phẩm (nhiều ảnh)")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new string[] { ".jpg", ".png", ".jpeg", ".gif" }, ErrorMessage = "Chỉ chấp nhận file ảnh (jpg, png, jpeg, gif)")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Tổng kích thước files không được vượt quá 5MB")]
        public List<IFormFile> ProductImages { get; set; } = new List<IFormFile>();

        [Required(ErrorMessage = "Danh mục là bắt buộc")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [Required(ErrorMessage = "Thương hiệu là bắt buộc")]
        [Display(Name = "Thương hiệu")]
        public int BrandId { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand Brand { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ảnh sản phẩm")]
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }

    }
}
