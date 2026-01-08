using FlightClientApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using static System.Net.WebRequestMethods;

namespace FlightClientApp.Services
{
    public class FlightsAPIService : IFlightsAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FlightsAPIService> _logger;

        public FlightsAPIService(
            HttpClient httpClient,
            ILogger<FlightsAPIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Flight?> GetFlightByNumberAsync(string number, CancellationToken ct = default)
        {
            return await ExecuteRequestAsync<Flight>($"api/flights/{Uri.EscapeDataString(number)}", ct);
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(string dateIso, CancellationToken ct = default)
        {
            return await ExecuteRequestAsync<List<Flight>>($"api/flights?date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDepartureAndDateAsync(string city, string dateIso, CancellationToken ct = default)
        {
            return await ExecuteRequestAsync<List<Flight>>($"api/flights/departure?city={Uri.EscapeDataString(city)}&date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
        }


        public async Task<IReadOnlyList<Flight>> GetFlightsByArrivalAndDateAsync(string city, string dateIso, CancellationToken ct = default)
        {
            return await ExecuteRequestAsync<List<Flight>>($"api/flights/arrival?city={Uri.EscapeDataString(city)}&date={Uri.EscapeDataString(dateIso)}", ct) ?? [];
        }

        private async Task<T?> ExecuteRequestAsync<T>(string url, CancellationToken ct)
        {
            using var resp = await _httpClient.GetAsync(url, ct);

            if (resp.IsSuccessStatusCode)
            {
                return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            }

            ProblemDetails? problem = null;
            try
            {
                problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            }
            catch { }

            throw new APIException(resp.StatusCode, problem);
        }
    }
}
