using FlightStorageService.Models;
using FlightStorageService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FlightStorageService.Controllers
{
    [Route("api/flights")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightService _service;
        private readonly ILogger<FlightsController> _log;

        public FlightsController(IFlightService service, ILogger<FlightsController> log)
        {
            _service = service;
            _log = log;
        }

        [HttpGet("{flightNumber}")]
        [SwaggerOperation(
                Summary = "Отримати рейс за номером",
                Description = "Повертає один авіарейс за унікальним номером, використовуючи збережену процедуру dbo.GetFlightByNumber")]
        [ProducesResponseType(typeof(Flight), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByNumber(string flightNumber, CancellationToken ct)
        {
            _log.LogInformation("Fetching from DB...");
            var flight = await _service.GetFlightByNumberAsync(flightNumber, ct);
            if (flight is null)
                return NotFound();
            return Ok(flight);
        }

        [HttpGet]
        [SwaggerOperation(
                Summary = "Отримати рейс за датою (UTC)",
                Description = "Повертає список авіарейсів за датою вильоту, використовуючи збережену процедуру dbo.GetFlightsByDate")]
        [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByDate([FromQuery] string date, CancellationToken ct)
        {
            return Ok(await _service.GetFlightsByDateAsync(date, ct));
        }

        [HttpGet("departure")]
        [SwaggerOperation(
                Summary = "Отримати рейс за містом вильоту та датою",
                Description = "Повертає список авіарейсів за містом вильоту та датою, використовуючи збережену процедуру dbo.GetFlightsByDepartureCityAndDate")]
        [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByDeparture([FromQuery] string city, [FromQuery] string date, CancellationToken ct)
        {
            return Ok(await _service.GetFlightsByDepartureAndDateAsync(city, date, ct));
        }

        [HttpGet("arrival")]
        [SwaggerOperation(
                Summary = "Отримати рейс за містом прильоту та датою",
                Description = "Повертає список авіарейсів за містом призначення та датою вильоту, використовуючи збережену процедуру dbo.GetFlightsByArrivalCityAndDate")]
        [ProducesResponseType(typeof(IEnumerable<Flight>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByArrival([FromQuery] string city, [FromQuery] string date, CancellationToken ct)
        {
            return Ok(await _service.GetFlightsByArrivalAndDateAsync(city, date, ct));
        }
    }
}
