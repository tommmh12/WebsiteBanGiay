using System.ComponentModel.DataAnnotations;

namespace WebsiteBanGiay.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required, Range(1, 100)]
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
