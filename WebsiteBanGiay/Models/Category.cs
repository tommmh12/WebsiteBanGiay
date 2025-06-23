using System.ComponentModel.DataAnnotations;

namespace WebsiteBanGiay.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
