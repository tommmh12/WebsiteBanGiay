using System.ComponentModel.DataAnnotations;

namespace WebsiteBanGiay.Models
{
    public class ReviewImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public string Url { get; set; }

        [Required]
        public int ReviewId { get; set; }
        public Review Review { get; set; }
    }
}
