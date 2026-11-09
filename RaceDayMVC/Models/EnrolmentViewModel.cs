namespace RaceDayMVC.Models
{
    public class EnrolmentViewModel
    {
        public int EnrolmentID { get; set; }

        public int EventID { get; set; }

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string ParticipantName { get; set; } = string.Empty;

        public string ParticipantEmail { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }
    }
}