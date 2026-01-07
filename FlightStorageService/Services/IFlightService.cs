using FlightStorageService.Models;

namespace FlightStorageService.Services
{
    public interface IFlightService
    {
        Task<Flight?> GetFlightByNumberAsync(string flightNumber, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(string dateIso, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByDepartureAndDateAsync(string city, string dateIso, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByArrivalAndDateAsync(string city, string dateIso, CancellationToken ct);
    }
}
