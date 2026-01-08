using FlightStorageService.Models;
using FlightStorageService.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace FlightStorageService.Services
{
    public class FlightService : IFlightService
    {
        private readonly IFlightRepository _repo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FlightService> _log;
        private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(2);

        public FlightService(IFlightRepository repo, IMemoryCache cache, ILogger<FlightService> log)
        {
            _repo = repo;
            _cache = cache;
            _log = log;
        }

        public async Task<Flight?> GetFlightByNumberAsync(string flightNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                _log.LogWarning("GetByNumberAsync called with empty flight number");
                throw new ArgumentException("Flight number is required.", nameof(flightNumber));
            }

            var number = flightNumber.Trim().ToUpperInvariant();

            var key = $"flight:num:{number}";

            if (_cache.TryGetValue(key, out Flight? cachedFlight))
            {
                _log.LogInformation("Cache HIT for flight number: {FlightNumber}", number);
                return cachedFlight;
            }

            _log.LogInformation("Cache MISS for flight number: {FlightNumber}", number);

            try
            {
                var flight = await _repo.GetFlightByNumberAsync(number, ct);

                if (flight is not null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = AbsoluteTtl,
                        SlidingExpiration = SlidingTtl
                    };

                    _cache.Set(key, flight, cacheOptions);
                    _log.LogInformation("Flight cached: {FlightNumber}, TTL: {Ttl}", number, AbsoluteTtl);
                }
                else
                {
                    _log.LogInformation("Flight not found: {FlightNumber}", number);
                }

                return flight;
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("GetByNumberAsync cancelled for flight: {FlightNumber}", number);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving flight: {FlightNumber}", number);
                throw;
            }

        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(string dateIso, CancellationToken ct = default)
        {
            var parsedDate = ParseDate(dateIso);
            var key = $"flights:date:{parsedDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(key, out IReadOnlyList<Flight>? cachedFlights))
            {
                _log.LogInformation("Cache HIT for date: {ParsedDate}", parsedDate);
                return cachedFlights!;
            }

            _log.LogInformation("Cache MISS for date: {ParsedDate}", parsedDate);

            try
            {
                var flights = await _repo.GetFlightsByDateAsync(parsedDate, ct);

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AbsoluteTtl,  
                    SlidingExpiration = SlidingTtl   
                };

                _cache.Set(key, flights, cacheOptions);

                if (flights.Count > 0)
                {
                    _log.LogInformation("Flights cached for date: {Date}, Count: {Count}, TTL: {Ttl}",
                        parsedDate, flights.Count, AbsoluteTtl);
                }
                else
                {
                    _log.LogInformation("No flights found for date: {Date} (empty list cached)", parsedDate);
                }

                return flights;
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("GetFlightsByDateAsync cancelled for flights: {ParsedDate}", parsedDate);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving flights: {ParsedDate}", parsedDate);
                throw;
            }
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDepartureAndDateAsync(string city, string dateIso, CancellationToken ct = default)
        {
            var normCity = ValidateAndNormalizeCity(city);
            var parsedDate = ParseDate(dateIso);

            var key = $"flights:dep:{normCity.ToUpperInvariant()}:{parsedDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(key, out IReadOnlyList<Flight>? cachedFlights))
            {
                _log.LogInformation("Cache HIT for departure: {City} on {Date}, Count: {Count}",
                    normCity, parsedDate, cachedFlights?.Count ?? 0);
                return cachedFlights!;
            }

            _log.LogInformation("Cache MISS for departure: {City} on {Date}", normCity, parsedDate);

            try
            {
                var flights = await _repo.GetFlightsByDepartureCityAndDateAsync(normCity, parsedDate, ct);

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AbsoluteTtl,  
                    SlidingExpiration = SlidingTtl                
                };

                _cache.Set(key, flights, cacheOptions);

                if (flights.Count > 0)
                {
                    _log.LogInformation("Flights cached for departure: {City} on {Date}, Count: {Count}",
                        normCity, parsedDate, flights.Count);
                }
                else
                {
                    _log.LogInformation("No flights found for departure: {City} on {Date}, empty list cached",
                        normCity, parsedDate);
                }

                return flights;
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("GetFlightsByDepartureAndDateAsync cancelled for: {City} on {Date}", normCity, parsedDate);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving flights for departure: {City} on {Date}", normCity, parsedDate);
                throw;
            }

        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByArrivalAndDateAsync(string city, string dateIso, CancellationToken ct = default)
        {
            var normCity = ValidateAndNormalizeCity(city);
            var parsedDate = ParseDate(dateIso);

            var key = $"flights:arr:{normCity.ToUpperInvariant()}:{parsedDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(key, out IReadOnlyList<Flight>? cachedFlights))
            {
                _log.LogInformation("Cache HIT for arrival: {City} on {Date}, Count: {Count}",
                    normCity, parsedDate, cachedFlights?.Count ?? 0);
                return cachedFlights!;
            }

            _log.LogInformation("Cache MISS for arrival: {City} on {Date}", normCity, parsedDate);

            try
            {
                var flights = await _repo.GetFlightsByArrivalCityAndDateAsync(normCity, parsedDate, ct);

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AbsoluteTtl,
                    SlidingExpiration = SlidingTtl
                };

                _cache.Set(key, flights, cacheOptions);

                if (flights.Count > 0)
                {
                    _log.LogInformation("Flights cached for arrival: {City} on {Date}, Count: {Count}",
                        normCity, parsedDate, flights.Count);
                }
                else
                {
                    _log.LogInformation("No flights found for arrival: {City} on {Date}, empty list cached",
                        normCity, parsedDate);
                }

                return flights;
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("GetFlightsByArrivalAndDateAsync cancelled for: {City} on {Date}", normCity, parsedDate);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving flights for arrival: {City} on {Date}", normCity, parsedDate);
                throw;
            }
        }

        #region Helpers
        private string ValidateAndNormalizeCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                _log.LogWarning("ValidateCity: City is null or empty.");
                throw new ArgumentException("City is required.", nameof(city));
            }

            var cityTrimmed = city.Trim();

            if (cityTrimmed.Length > 100)
            {
                _log.LogWarning("ValidateCity: City name too long. Length: {Length}, Max: 100, Value: {City}",
                    cityTrimmed.Length, cityTrimmed);
                throw new ArgumentException($"City name is too long (max 100 characters, got {cityTrimmed.Length}).", nameof(city));
            }

            _log.LogDebug("ValidateCity: Valid city name: {City}", cityTrimmed);

            return cityTrimmed;
        }

        private DateOnly ParseDate(string? dateIso)
        {
            if (string.IsNullOrWhiteSpace(dateIso))
            {
                _log.LogWarning("ParseDate called with null or empty date string");
                throw new ArgumentException("Date is required. Expected format: yyyy-MM-dd.", nameof(dateIso));
            }

            var dateTrimmed = dateIso.Trim();

            if (!DateOnly.TryParseExact(dateTrimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                _log.LogWarning("Failed to parse date: {DateString}. Expected format: yyyy-MM-dd", dateTrimmed);
                throw new ArgumentException($"Invalid date '{dateTrimmed}'. Expected format: yyyy-MM-dd.", nameof(dateIso));
            }

            _log.LogDebug("Successfully parsed date: {DateString} -> {ParsedDate}", dateTrimmed, parsedDate);

            return parsedDate;
        }
        #endregion
    }
}
