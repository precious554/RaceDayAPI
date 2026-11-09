using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class EventViewModel
    {
        public int EventID { get; set; }

        [Required]
        [StringLength(100)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.1, 1000)]
        public decimal Distance { get; set; }

        [Required]
        public string EventType { get; set; } = "Run";

        public string? ImageUrl { get; set; }
    }
}