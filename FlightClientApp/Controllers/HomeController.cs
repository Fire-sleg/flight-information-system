using FlightClientApp.Models;
using FlightClientApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FlightClientApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFlightsAPIService _api;
        private readonly ILogger<HomeController> _log;

        public HomeController(IFlightsAPIService api, ILogger<HomeController> log)
        {
            _api = api;
            _log = log;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> GetFlightByNumber(string flightNumber, CancellationToken ct)
        {
            return await HandleApiCallSafelyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(flightNumber))
                {
                    return new List<Flight>();
                }

                flightNumber = flightNumber.Trim();
                var flight = await _api.GetFlightByNumberAsync(flightNumber, ct);

                if (flight == null)
                {
                    return new List<Flight>();
                }

                return new List<Flight> { flight };
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetFlightsByDate(string date, CancellationToken ct)
        {
            return await HandleApiCallSafelyAsync(() => _api.GetFlightsByDateAsync(date, ct));
        }

        [HttpPost]
        public async Task<IActionResult> GetFlightsByDepartureAndDate(string city, string date, CancellationToken ct)
        {
            return await HandleApiCallSafelyAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return Task.FromResult<IReadOnlyList<Flight>>(new List<Flight>());
                }
                return _api.GetFlightsByDepartureAndDateAsync(city.Trim(), date, ct);
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetFlightsByArrivalAndDate(string city, string date, CancellationToken ct)
        {
            return await HandleApiCallSafelyAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return Task.FromResult<IReadOnlyList<Flight>>(new List<Flight>());
                }
                return _api.GetFlightsByArrivalAndDateAsync(city.Trim(), date, ct);
            });
        }


        private async Task<IActionResult> HandleApiCallSafelyAsync(Func<Task<IReadOnlyList<Flight>>> apiAction)
        {
            var viewModel = new FlightSearchResultViewModel();
            try
            {
                viewModel.Flights = await apiAction();
                return View("Results", viewModel);
            }
            catch (APIException apiEx)
            {
                var statusCode = (int)apiEx.StatusCode;
                var errorTitle = apiEx.Problem?.Title ?? apiEx.Message;
                var errorDetail = apiEx.Problem?.Detail;
                var traceId = apiEx.Problem?.Extensions.TryGetValue("traceId", out var t) == true ? t?.ToString() : null;

                var statusCodeMessage = apiEx.StatusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest =>
                        "Invalid search parameters. Please check your input.",

                    System.Net.HttpStatusCode.NotFound =>
                        "No flights found for the given criteria.",

                    System.Net.HttpStatusCode.TooManyRequests =>
                        "Too many requests. Please try again later.",

                    System.Net.HttpStatusCode.InternalServerError =>
                        "Server error. Please try again later.",

                    _ => "Unexpected server error."
                };


                var errorMessage = $"{statusCode} {errorTitle}. {statusCodeMessage}";
                if (!string.IsNullOrEmpty(errorDetail))
                {
                    errorMessage += $" Details: {errorDetail}";
                }
                if (!string.IsNullOrEmpty(traceId))
                {
                    errorMessage += $" (Trace ID: {traceId})";
                }

                viewModel.StatusCode = statusCode;
                viewModel.ErrorMessage = statusCodeMessage;

                return View("Results", viewModel);
            }
            catch (HttpRequestException)
            {
                viewModel.StatusCode = 503;
                viewModel.ErrorMessage = "Unable to connect to the server. Please try again later.";
                return View("Results", viewModel);
            }
            catch (Exception)
            {
                viewModel.StatusCode = 500;
                viewModel.ErrorMessage = "Unexpected application error.";
                return View("Results", viewModel);
            }
        }
    }
}
