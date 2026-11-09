namespace RaceDayMVC.Models
{
    public class ResultViewModel
    {
        public int ResultID { get; set; }

        public int EventID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public string ResultStatus { get; set; } = string.Empty;
    }
}