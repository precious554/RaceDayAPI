namespace RaceDayAPI.Models
{
    public class Route
    {
        public int RouteID { get; set; }
        public int EventID { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string? Description { get; set; }
        public string? RouteMapURL { get; set; }
    }
}
