using Container_Testing.Models.Context;
using Container_Testing.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Container_Testing.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly ETicketingContext _eTicketingContext;

        public AuthController(ETicketingContext eTicketingContext)
        {
            _eTicketingContext = eTicketingContext;
        }

        // dont forget to change session timeout
        [HttpPost("login")]
        public async Task<ActionResult<ResponseModels<UserModels>>> LoginUser([FromBody] UserModels inputUserData)
        {
            try
            {
                ResponseModels<UserModels> response = new();

                if (inputUserData.UserEmail.IsNullOrEmpty() || inputUserData.UserPassword.IsNullOrEmpty())
                {

                    return BadRequest(new ResponseModels<UserModels>
                    {
                        StatusCode = 400,
                        Message = "Data cannot be empty!"
                    });
                }

                var userData = await _eTicketingContext.CatalogUser.AsNoTracking().Where(x => x.UserEmail.Equals(inputUserData.UserEmail) && x.UserPassword.Equals(inputUserData.UserPassword)).ToListAsync();

                if (userData == null || userData.Count.Equals(0))
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "Your login information is invalid!"
                    });
                }

                HttpContext.Session.SetString("LoginInfo", JsonSerializer.Serialize(userData));

                response.StatusCode = 200;
                response.Message = "Successfully logged in!";
                response.Data = userData;

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

        [HttpGet("get-session")]
        public async Task<ActionResult<ResponseModels<UserModels>>> GetSessionData()
        {
            try
            {
                ResponseModels<UserModels> response = new();

                // Retrieve session data
                var sessionData = HttpContext.Session.GetString("LoginInfo");

                if (string.IsNullOrEmpty(sessionData))
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "No active session found!"
                    });
                }

                var userData = JsonSerializer.Deserialize<List<UserModels>>(sessionData);

                response.StatusCode = 200;
                response.Message = "Session retrieved!";
                response.Data = userData;

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

        [HttpDelete("logout")]
        public async Task<ActionResult<ResponseModels<UserModels>>> LogoutUser()
        {
            try
            {
                ResponseModels<UserModels> response = new();

                HttpContext.Session.Clear();

                var sessionData = HttpContext.Session.GetString("LoginInfo");

                if (!string.IsNullOrEmpty(sessionData))
                {
                    var userData = JsonSerializer.Deserialize<List<UserModels>>(sessionData);

                    return BadRequest(new ResponseModels<UserModels>
                    {
                        StatusCode = 400,
                        Message = "Unable to logout!",
                        Data = userData
                    });
                }

                response.StatusCode = 200;
                response.Message = "Successfully logged out!";

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
    }
}
