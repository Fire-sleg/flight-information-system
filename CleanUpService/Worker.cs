using Microsoft.Data.SqlClient;
using System.Data;

namespace CleanUpService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _log;
        private readonly string _connectionString;
        private readonly int _timeout;
        public Worker(ILogger<Worker> log, IConfiguration config)
        {
            _log = log;
            _connectionString = config.GetConnectionString("FlightsDb")
               ?? throw new InvalidOperationException("Connection string missing");
            _timeout = config.GetValue<int>("CleanUpTimeDelay:Days");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteProcedureAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromDays(_timeout), stoppingToken);
            }
        }
        public async Task ExecuteProcedureAsync(CancellationToken ct)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await using var command = new SqlCommand("dbo.CleanupOldFlights", connection)
                { CommandType = CommandType.StoredProcedure };

                await connection.OpenAsync(ct);

                await command.ExecuteNonQueryAsync(ct);

                _log.LogInformation("CleanupOldFlights executed at: {time}", DateTimeOffset.Now);
            }
            
            catch (Exception ex)
            {
                _log.LogError(ex, "Error during cleanup old flights");
            }
        }
    }
}
