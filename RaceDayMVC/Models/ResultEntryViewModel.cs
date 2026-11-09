using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class ResultEntryViewModel
    {
        public int EnrolmentID { get; set; }

        public int EventID { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string FinishTime { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int? Position { get; set; }

        [Required]
        public string ResultStatus { get; set; } = "Completed";
    }
}