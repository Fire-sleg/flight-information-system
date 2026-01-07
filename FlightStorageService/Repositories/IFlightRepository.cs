using FlightStorageService.Models;

namespace FlightStorageService.Repositories
{
    public interface IFlightRepository
    {
        Task<Flight?> GetFlightByNumberAsync(string flightNumber, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(DateOnly date, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByDepartureCityAndDateAsync(string city, DateOnly date, CancellationToken ct);
        Task<IReadOnlyList<Flight>> GetFlightsByArrivalCityAndDateAsync(string city, DateOnly date, CancellationToken ct);
    }
}
