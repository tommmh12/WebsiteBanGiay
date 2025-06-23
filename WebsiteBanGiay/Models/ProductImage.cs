using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanGiay.Models
{
    public class ProductImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImageId { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh là bắt buộc")]
        [Display(Name = "Đường dẫn ảnh")]
        public string Url { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public bool IsMain { get; set; } = false;

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; } = 0;

        [Required(ErrorMessage = "Sản phẩm là bắt buộc")]
        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
