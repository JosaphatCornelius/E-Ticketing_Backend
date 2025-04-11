using Container_Testing.Models;
using Container_Testing.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Container_Testing.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class AirlineController : Controller
    {
        private readonly ETicketingContext _eTicketingContext;

        public AirlineController(ETicketingContext eTicketingContext)
        {
            _eTicketingContext = eTicketingContext;
        }

        [HttpGet("get-airline-list")]
        public async Task<ActionResult<ResponseModels<AirlineModels>>> GetAirlineList()
        {
            try
            {
                ResponseModels<AirlineModels> response = new();

                var airlines = await _eTicketingContext.CatalogAirline.AsNoTracking().ToListAsync();

                if (airlines.IsNullOrEmpty())
                {
                    return NotFound(new ResponseModels<AirlineModels>
                    {
                        StatusCode = 404,
                        Message = "Airline not found!"
                    });
                }

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = airlines;

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<AirlineModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }

        [HttpPost("post-airline")]
        public async Task<ActionResult<ResponseModels<AirlineModels>>> PostAirline([FromBody] AirlineModels inputAirlineData)
        {
            AirlineModels newAirlineData = new();

            try
            {
                ResponseModels<AirlineModels> response = new();

                inputAirlineData.AirlineID = newAirlineData.AirlineID;

                await _eTicketingContext.CatalogAirline.AddAsync(inputAirlineData);

                await _eTicketingContext.SaveChangesAsync();

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = await _eTicketingContext.CatalogAirline.AsNoTracking().ToListAsync();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModels<AirlineModels>
                {
                    StatusCode = 500,
                    Message = "Internal server error: " + ex.Message
                });
            }
        }
    }
}
