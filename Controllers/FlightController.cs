﻿using Container_Testing.Models;
using Container_Testing.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Container_Testing.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class FlightController : Controller
    {
        private readonly ETicketingContext _eTicketingContext;

        public FlightController(ETicketingContext eTicketingContext)
        {
            _eTicketingContext = eTicketingContext;
        }

        [HttpGet("get-flight-list")]
        public async Task<ActionResult<ResponseModels<FlightModels>>> GetFlightList([FromQuery] string? from, [FromQuery] string? destination, [FromQuery] DateTime? departure)
        {
            try
            {
                ResponseModels<FlightModels> response = new();

                // Start with the base query
                var query = _eTicketingContext.CatalogFlight.AsNoTracking().AsQueryable();

                // Apply filters conditionally
                if (!string.IsNullOrEmpty(from))
                    query = query.Where(x => x.FlightFrom == from);

                if (!string.IsNullOrEmpty(destination))
                    query = query.Where(x => x.FlightDestination == destination);

                if (departure.HasValue)
                {
                    var date = departure.Value.Date;
                    var nextDate = date.AddDays(1);

                    query = query.Where(x => x.FlightTime >= date && x.FlightTime < nextDate);
                }

                var flights = await query.ToListAsync();

                if (flights == null || flights.Count == 0)
                {
                    return NotFound(new ResponseModels<FlightModels>
                    {
                        StatusCode = 404,
                        Message = "Flights not found!"
                    });
                }

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = flights;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<FlightModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("post-flight")]
        public async Task<ActionResult<ResponseModels<FlightModels>>> PostFlight([FromBody] FlightModels inputFlightData)
        {
            try
            {
                FlightModels newFlightData = new();

                ResponseModels<FlightModels> responseSuccess = new();

                inputFlightData.FlightID = newFlightData.FlightID;

                if (inputFlightData.AirlineID.IsNullOrEmpty() || await _eTicketingContext.CatalogAirline.AsNoTracking().FirstOrDefaultAsync(x => x.AirlineID.Equals(inputFlightData.AirlineID)) == null)
                {
                    return NotFound(new ResponseModels<FlightModels>
                    {
                        StatusCode = 404,
                        Message = "Airline not found!"
                    });
                }

                await _eTicketingContext.CatalogFlight.AddAsync(inputFlightData);

                await _eTicketingContext.SaveChangesAsync();

                responseSuccess.StatusCode = 200;
                responseSuccess.Message = "Success";
                responseSuccess.Data = await _eTicketingContext.CatalogFlight.AsNoTracking().ToListAsync();

                return Ok(responseSuccess);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<FlightModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPatch("update-flight")]
        public async Task<ActionResult<ResponseModels<FlightModels>>> PatchFlight(string flightID, [FromBody] FlightModels updatedFlight)
        {
            try
            {
                var existingFlight = await _eTicketingContext.CatalogFlight.FirstOrDefaultAsync(f => f.FlightID == flightID);
                if (existingFlight == null)
                {
                    return NotFound(new ResponseModels<FlightModels> { StatusCode = 404, Message = "Flight not found!" });
                }

                // Flight info updates
                if (!string.IsNullOrEmpty(updatedFlight.FlightDestination))
                    existingFlight.FlightDestination = updatedFlight.FlightDestination;

                if (!string.IsNullOrEmpty(updatedFlight.FlightFrom))
                    existingFlight.FlightFrom = updatedFlight.FlightFrom;

                if (updatedFlight.FlightTime.HasValue)
                    existingFlight.FlightTime = updatedFlight.FlightTime;

                if (updatedFlight.FlightArrival.HasValue)
                    existingFlight.FlightArrival = updatedFlight.FlightArrival;

                if (updatedFlight.FlightSeat.HasValue)
                    existingFlight.FlightSeat = updatedFlight.FlightSeat;

                if (updatedFlight.FlightPrice.HasValue)
                    existingFlight.FlightPrice = updatedFlight.FlightPrice;

                await _eTicketingContext.SaveChangesAsync();

                return Ok(new ResponseModels<FlightModels>
                {
                    StatusCode = 200,
                    Message = "Flight updated successfully.",
                    Data = new List<FlightModels> { existingFlight }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<FlightModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpDelete("delete-flight")]
        public async Task<ActionResult<ResponseModels<FlightModels>>> DeleteFlight([FromQuery] string flightID)
        {
            try
            {
                var response = new ResponseModels<FlightModels>();

                if (string.IsNullOrEmpty(flightID))
                {
                    return BadRequest(new ResponseModels<FlightModels>
                    {
                        StatusCode = 400,
                        Message = "FlightID cannot be empty!"
                    });
                }

                var flight = await _eTicketingContext.CatalogFlight.FirstOrDefaultAsync(x => x.FlightID == flightID);

                if (flight == null)
                {
                    return NotFound(new ResponseModels<FlightModels>
                    {
                        StatusCode = 404,
                        Message = "Flight not found!"
                    });
                }

                var bookingData = await _eTicketingContext.CatalogBooking.FirstOrDefaultAsync(x => x.FlightID == flightID);

                if (bookingData != null)
                {
                    return BadRequest(new ResponseModels<FlightModels>
                    {
                        StatusCode = 400,
                        Message = "There's an active booking/ticket on this flight!"
                    });
                }

                _eTicketingContext.CatalogFlight.Remove(flight);
                await _eTicketingContext.SaveChangesAsync();

                response.StatusCode = 200;
                response.Message = "Flight deleted successfully!";
                response.Data = await _eTicketingContext.CatalogFlight.AsNoTracking().ToListAsync();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<FlightModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }
    }
}
