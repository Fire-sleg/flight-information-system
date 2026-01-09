namespace FlightClientApp.Models
{
    public class FlightSearchResultViewModel
    {
        public IReadOnlyList<Flight> Flights { get; set; } = new List<Flight>();

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public string? ErrorMessage { get; set; }

        public int? StatusCode { get; set; }
    }
}
