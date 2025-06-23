using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;


namespace WebsiteBanGiay.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required, StringLength(255)]
        public string Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thêm thuộc tính DefaultRole với giá trị mặc định
        public string DefaultRole { get; set; } = "Customer";

        // Computed property
        public string FullName => $"{FirstName} {LastName}";

        // Navigation properties
        public ICollection<Order> Orders { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}