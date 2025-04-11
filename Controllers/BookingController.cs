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

                await _eTicketingContext.CatalogBooking.AddAsync(inputBookingData);

                await _eTicketingContext.SaveChangesAsync();

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = await _eTicketingContext.CatalogBooking.AsNoTracking().ToListAsync();

                return Ok(response);
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
