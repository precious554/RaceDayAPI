using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 1000)]
        public decimal Distance { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }
    }
}