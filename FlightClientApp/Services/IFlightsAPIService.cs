using FlightClientApp.Models;

namespace FlightClientApp.Services
{
    public interface IFlightsAPIService
    {
        Task<Flight?> GetFlightByNumberAsync(string flightNumber, CancellationToken ct = default);
        Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(string dateIso, CancellationToken ct = default);
        Task<IReadOnlyList<Flight>> GetFlightsByDepartureAndDateAsync(string city, string dateIso, CancellationToken ct = default);
        Task<IReadOnlyList<Flight>> GetFlightsByArrivalAndDateAsync(string city, string dateIso, CancellationToken ct = default);
    }
}
