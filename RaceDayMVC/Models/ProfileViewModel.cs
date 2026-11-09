using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}