using FlightStorageService.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightStorageService.Repositories
{
    public class FlightRepository : IFlightRepository
    {
        private readonly string _connectionString;
        public FlightRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("FlightsDb")!;
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByArrivalCityAndDateAsync(string city, DateOnly date, CancellationToken ct)
        {
            return await QueryListAsync("dbo.GetFlightsByArrivalCityAndDate", new[]
               {
                    new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = city },
                    new SqlParameter("@Date", SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) }
                },
               ct);
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDateAsync(DateOnly date, CancellationToken ct)
        {
            return await QueryListAsync("dbo.GetFlightsByDate",
                new SqlParameter("@Date", SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) },
                ct);
        }

        public async Task<IReadOnlyList<Flight>> GetFlightsByDepartureCityAndDateAsync(string city, DateOnly date, CancellationToken ct)
        {
            return await QueryListAsync("dbo.GetFlightsByDepartureCityAndDate", new[]
                {
                    new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = city },
                    new SqlParameter("@Date", SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) }
                },
                ct);
        }

        public async Task<Flight?> GetFlightByNumberAsync(string flightNumber, CancellationToken ct)
        {
            return await QuerySingleAsync("@dbo.GetFlightByNumber", 
                new SqlParameter("@FlightNumber", SqlDbType.NVarChar, 10) { Value =  flightNumber },
                ct);
        }
        private async Task<Flight> QuerySingleAsync(string storedProcedure, SqlParameter parameters, CancellationToken ct)
        {
            return await QuerySingleAsync(storedProcedure, new[] { parameters }, ct);
        }

        private async Task<IReadOnlyList<Flight>> QueryListAsync(string storedProcedure, SqlParameter parameters, CancellationToken ct)
        {
            return await QueryListAsync(storedProcedure, new[] { parameters }, ct);
        }

        private async Task<Flight?> QuerySingleAsync(
            string storedProcedure,
            SqlParameter[] parameters,
            CancellationToken ct)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(storedProcedure, connection)
            { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync(ct);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);

            if (!await reader.ReadAsync(ct)) return null;
            return MapToFlight(reader);
        }

        private async Task<IReadOnlyList<Flight>> QueryListAsync(
            string storedProcedure,
            SqlParameter[] parameters,
            CancellationToken ct)
        {
            var flights = new List<Flight>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(storedProcedure, connection)
            { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync(ct);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);

            while (await reader.ReadAsync(ct))
            {
                flights.Add(MapToFlight(reader));
            }

            return flights;
        }
        private static Flight MapToFlight(SqlDataReader reader)
        {
            return new Flight
            {
                FlightNumber = reader.GetString(reader.GetOrdinal("FlightNumber")),
                DepartureDateTime = reader.GetDateTime(reader.GetOrdinal("DepartureDateTime")),
                DepartureAirportCity = reader.GetString(reader.GetOrdinal("DepartureAirportCity")),
                ArrivalAirportCity = reader.GetString(reader.GetOrdinal("ArrivalAirportCity")),
                DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes"))
            };
        }

    }

}
