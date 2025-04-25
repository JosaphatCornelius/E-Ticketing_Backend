using Container_Testing.Models;
using Container_Testing.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Container_Testing.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class BookingController : Controller
    {
        private readonly ETicketingContext _eTicketingContext;

        public BookingController(ETicketingContext eTicketingContext)
        {
            _eTicketingContext = eTicketingContext;
        }

        [HttpGet("get-booking-list")]
        public async Task<ActionResult<ResponseModels<BookingModels>>> GetBookingList([FromQuery] string? userID)
        {
            try
            {
                ResponseModels<BookingModels> response = new();

                List<BookingModels> bookings;

                if (userID != null)
                {
                    bookings = await _eTicketingContext.CatalogBooking.AsNoTracking().Where(x => x.UserID.Equals(userID)).ToListAsync();
                }
                else
                {
                    bookings = await _eTicketingContext.CatalogBooking.AsNoTracking().ToListAsync();
                }

                if (bookings.IsNullOrEmpty())
                {
                    return NotFound(new ResponseModels<BookingModels>
                    {
                        StatusCode = 404,
                        Message = "Booking not found!"
                    });
                }

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = bookings;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<UserModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("post-booking")]
        public async Task<ActionResult<ResponseModels<BookingModels>>> PostBooking([FromBody] BookingModels inputBookingData)
        {
            BookingModels newBookingData = new();

            try
            {
                ResponseModels<BookingModels> response = new();

                inputBookingData.BookingID = newBookingData.BookingID;

                var flightData = await _eTicketingContext.CatalogFlight.FirstOrDefaultAsync(x => x.FlightID.Equals(inputBookingData.FlightID));

                var bookingData = await _eTicketingContext.CatalogBooking.AsNoTracking().FirstOrDefaultAsync(x => x.FlightID.Equals(inputBookingData.FlightID) && x.UserID.Equals(inputBookingData.UserID));

                if (flightData == null)
                {
                    return NotFound(new ResponseModels<BookingModels>
                    {
                        StatusCode = 404,
                        Message = "Flight not found!"
                    });
                }

                if (bookingData != null)
                {
                    if (!bookingData.BookingConfirmation.Equals("denied"))
                    {
                        return BadRequest(new ResponseModels<BookingModels>
                        {
                            StatusCode = 400,
                            Message = "You've already booked this flight!"
                        });
                    }
                }

                TimeSpan? dateDiff = flightData.FlightTime - DateTime.Now;

                if (dateDiff.HasValue && dateDiff.Value.TotalDays < 2)
                {
                    return BadRequest(new ResponseModels<BookingModels>
                    {
                        StatusCode = 400,
                        Message = "You can't book a flight a day before!"
                    });
                }

                if (flightData.FlightSeat > 0 && flightData.FlightSeat >= inputBookingData.SeatAmount)
                {
                    flightData.FlightSeat = flightData.FlightSeat - inputBookingData.SeatAmount;

                    await _eTicketingContext.CatalogBooking.AddAsync(inputBookingData);

                    await _eTicketingContext.SaveChangesAsync();

                    response.StatusCode = 200;
                    response.Message = "Success";
                    response.Data = await _eTicketingContext.CatalogBooking.AsNoTracking().ToListAsync();

                    return Ok(response);
                }
                else
                {
                    return BadRequest(new ResponseModels<BookingModels>
                    {
                        StatusCode = 400,
                        Message = "You're ordering more seat than it is available!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<BookingModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPatch("patch-booking")]
        public async Task<ActionResult<ResponseModels<BookingModels>>> PatchBooking([FromBody] BookingModels updatedBooking)
        {
            try
            {
                if (string.IsNullOrEmpty(updatedBooking.BookingID))
                {
                    return BadRequest(new ResponseModels<BookingModels>
                    {
                        StatusCode = 400,
                        Message = "BookingID is required for patching."
                    });
                }

                var existingBooking = await _eTicketingContext.CatalogBooking.FirstOrDefaultAsync(b => b.BookingID == updatedBooking.BookingID);
                if (existingBooking == null)
                {
                    return NotFound(new ResponseModels<BookingModels>
                    {
                        StatusCode = 404,
                        Message = "Booking not found!"
                    });
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updatedBooking.UserID))
                    existingBooking.UserID = updatedBooking.UserID;

                if (!string.IsNullOrEmpty(updatedBooking.FlightID))
                    existingBooking.FlightID = updatedBooking.FlightID;

                if (updatedBooking.BookingPrice.HasValue)
                    existingBooking.BookingPrice = updatedBooking.BookingPrice;

                if (updatedBooking.SeatAmount.HasValue)
                    existingBooking.SeatAmount = updatedBooking.SeatAmount;

                if (!string.IsNullOrEmpty(updatedBooking.PaymentStatus))
                    existingBooking.PaymentStatus = updatedBooking.PaymentStatus;

                if (!string.IsNullOrEmpty(updatedBooking.BookingConfirmation))
                    existingBooking.BookingConfirmation = updatedBooking.BookingConfirmation;

                var existingFlight = await _eTicketingContext.CatalogFlight.FirstOrDefaultAsync(f => f.FlightID == updatedBooking.FlightID);
                if (existingFlight == null)
                {
                    return NotFound(new ResponseModels<FlightModels> { StatusCode = 404, Message = "Flight not found!" });
                }

                var existingAirline = await _eTicketingContext.CatalogAirline.FirstOrDefaultAsync(f => f.AirlineID == existingFlight.AirlineID);
                if (existingFlight == null)
                {
                    return NotFound(new ResponseModels<AirlineModels> { StatusCode = 404, Message = "Airline not found!" });
                }

                if (updatedBooking.BookingConfirmation.Equals("confirmed"))
                {
                    if (!existingAirline.TicketSold.HasValue)
                    {
                        existingAirline.TicketSold = updatedBooking.SeatAmount;
                    }
                    else
                    {
                        existingAirline.TicketSold = existingAirline.TicketSold + updatedBooking.SeatAmount;
                    }
                }

                if (updatedBooking.BookingConfirmation.Equals("denied"))
                {
                    existingFlight.FlightSeat = existingFlight.FlightSeat + updatedBooking.SeatAmount;
                }

                await _eTicketingContext.SaveChangesAsync();

                return Ok(new ResponseModels<BookingModels>
                {
                    StatusCode = 200,
                    Message = "Booking updated successfully.",
                    Data = new List<BookingModels> { existingBooking }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<BookingModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

    }
}
