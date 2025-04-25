using Azure;
using Container_Testing.Models;
using Container_Testing.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Container_Testing.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly ETicketingContext _eTicketingContext;

        public UserController(ETicketingContext eTicketingContext)
        {
            _eTicketingContext = eTicketingContext;
        }

        [HttpGet("get-user-list")]
        public async Task<ActionResult<ResponseModels<UserModels>>> GetUserList()
        {
            try
            {
                ResponseModels<UserModels> response = new();

                var users = await _eTicketingContext.CatalogUser.AsNoTracking().ToListAsync();

                if (users.IsNullOrEmpty())
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "User not found!"
                    });
                }

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = await _eTicketingContext.CatalogUser.AsNoTracking().ToListAsync();

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

        [HttpPost("post-user")]
        public async Task<ActionResult<ResponseModels<UserModels>>> PostUser([FromBody] UserModels inputUserData)
        {
            try
            {
                AirlineModels newAirlineData = new();

                var response = new ResponseModels<UserModels>();

                if (await _eTicketingContext.CatalogUser.AsNoTracking().AnyAsync(x => x.Username == inputUserData.Username))
                {
                    return BadRequest(new ResponseModels<UserModels>
                    {
                        StatusCode = 400,
                        Message = "This username already exists!"
                    });
                }

                // Set UserID dan simpan ke database
                inputUserData.UserID = Guid.NewGuid().ToString();
                await _eTicketingContext.CatalogUser.AddAsync(inputUserData);
                await _eTicketingContext.SaveChangesAsync();

                if (inputUserData.UserRole.Equals("airline"))
                {
                    newAirlineData.AirlineID = inputUserData.UserID;
                    newAirlineData.TicketSold = 0;

                    await _eTicketingContext.CatalogAirline.AddAsync(newAirlineData);
                    await _eTicketingContext.SaveChangesAsync();
                }

                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = await _eTicketingContext.CatalogUser.AsNoTracking().ToListAsync();

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

        [HttpDelete("delete-user")]
        public async Task<ActionResult<ResponseModels<UserModels>>> DeleteUser([FromQuery] string userID)
        {
            try
            {
                var response = new ResponseModels<UserModels>();

                if (string.IsNullOrEmpty(userID))
                {
                    return BadRequest(new ResponseModels<UserModels>
                    {
                        StatusCode = 400,
                        Message = "UserID cannot be empty!"
                    });
                }

                var user = await _eTicketingContext.CatalogUser.FirstOrDefaultAsync(x => x.UserID == userID);

                if (user == null)
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "User not found!"
                    });
                }

                if (user.UserRole.Equals("airline"))
                {
                    var airline = await _eTicketingContext.CatalogAirline.FirstOrDefaultAsync(x => x.AirlineID == userID);

                    _eTicketingContext.CatalogAirline.Remove(airline);
                }

                _eTicketingContext.CatalogUser.Remove(user);
                await _eTicketingContext.SaveChangesAsync();

                response.StatusCode = 200;
                response.Message = "User deleted successfully!";
                response.Data = await _eTicketingContext.CatalogUser.AsNoTracking().ToListAsync();

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

        [HttpDelete("delete-multiple-users")]
        public async Task<ActionResult<ResponseModels<UserModels>>> DeleteMultipleUsers([FromBody] List<string> userIDs)
        {
            try
            {
                var response = new ResponseModels<UserModels>();

                if (userIDs == null || !userIDs.Any())
                {
                    return BadRequest(new ResponseModels<UserModels>
                    {
                        StatusCode = 400,
                        Message = "UserID list cannot be empty!"
                    });
                }

                var usersToDelete = await _eTicketingContext.CatalogUser
                    .Where(u => userIDs.Contains(u.UserID))
                    .ToListAsync();

                if (!usersToDelete.Any())
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "No users found with the provided IDs!"
                    });
                }

                // Hapus airlines yang terkait jika user role adalah 'airline'
                var airlineUsers = usersToDelete
                    .Where(x => x.UserRole.Equals("airline", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.UserID) // asumsikan AirlineID == UserID
                    .ToList();

                if (airlineUsers.Any())
                {
                    var airlinesToDelete = await _eTicketingContext.CatalogAirline
                        .Where(a => airlineUsers.Contains(a.AirlineID))
                        .ToListAsync();

                    _eTicketingContext.CatalogAirline.RemoveRange(airlinesToDelete);
                }

                _eTicketingContext.CatalogUser.RemoveRange(usersToDelete);
                await _eTicketingContext.SaveChangesAsync();

                response.StatusCode = 200;
                response.Message = $"{usersToDelete.Count} user(s) deleted successfully!";
                response.Data = await _eTicketingContext.CatalogUser.AsNoTracking().ToListAsync();

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


        [HttpPatch("patch-user")]
        public async Task<ActionResult<ResponseModels<UserModels>>> PatchUser(string userID, [FromBody] UserModels updatedUser)
        {
            try
            {
                AirlineModels newAirlineData = new();

                var existingUser = await _eTicketingContext.CatalogUser.FirstOrDefaultAsync(u => u.UserID == userID);

                if (existingUser == null)
                {
                    return NotFound(new ResponseModels<UserModels>
                    {
                        StatusCode = 404,
                        Message = "User not found!"
                    });
                }

                // Update fields only if values are provided
                if (!string.IsNullOrEmpty(updatedUser.Username))
                    existingUser.Username = updatedUser.Username;

                if (!string.IsNullOrEmpty(updatedUser.UserPassword))
                    existingUser.UserPassword = updatedUser.UserPassword;

                if (!string.IsNullOrEmpty(updatedUser.UserRole))
                {
                    existingUser.UserRole = updatedUser.UserRole;

                    if (updatedUser.UserRole == "airline")
                    {
                        newAirlineData.AirlineID = updatedUser.UserID;
                        newAirlineData.TicketSold = 0;

                        await _eTicketingContext.CatalogAirline.AddAsync(newAirlineData);
                        await _eTicketingContext.SaveChangesAsync();
                    }
                    else
                    {
                        var airlineToDelete = await _eTicketingContext.CatalogAirline
                            .FirstOrDefaultAsync(a => a.AirlineID == updatedUser.UserID);

                        if (airlineToDelete != null)
                        {
                            _eTicketingContext.CatalogAirline.Remove(airlineToDelete);
                            await _eTicketingContext.SaveChangesAsync();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(updatedUser.UserEmail))
                    existingUser.UserEmail = updatedUser.UserEmail;

                if (!string.IsNullOrEmpty(updatedUser.UserAddress))
                    existingUser.UserAddress = updatedUser.UserAddress;

                if (updatedUser.Birthday.HasValue)
                    existingUser.Birthday = updatedUser.Birthday;

                await _eTicketingContext.SaveChangesAsync();

                return Ok(new ResponseModels<UserModels>
                {
                    StatusCode = 200,
                    Message = "User updated successfully.",
                    Data = new List<UserModels> { existingUser }
                });
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
