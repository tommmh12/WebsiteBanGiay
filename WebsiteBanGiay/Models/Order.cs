using System.ComponentModel.DataAnnotations;

namespace WebsiteBanGiay.Models
{
    public class Order
    {

        [Key]
        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string ShippingAddress { get; set; }

        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
