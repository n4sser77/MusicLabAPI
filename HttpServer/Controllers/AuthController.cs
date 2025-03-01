
using Backend.asp.Services.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IJwtService _jwtService;
    public AuthController(IUserManager userManager, IJwtService jwtService)
    {
        _jwtService = jwtService;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser([FromBody] LogInModel loginModel)
    {
        //// 🔹 Get Authorization header
        //var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        //if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        //{
        //    return Unauthorized(new { message = "Missing or invalid token" });
        //}
        //    var token = authHeader.Substring("Bearer ".Length);

        if (!ModelState.IsValid) return BadRequest(ModelState);

        User? user = await _userManager.LogInUser(loginModel);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        var token = await _jwtService.GenerateJwtToken(user.Id.ToString(), user.Role);
        var res = Ok(new ResponseModel { Token = token, Message = "User successfully logged in" });
        return res;

    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignupUser([FromBody] LogInModel logInModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        User newUser = new User { Email = logInModel.Email, Password = logInModel.Password };

        var user = await _userManager.CreateUser(newUser);

        if (user == null)
        {
            return Unauthorized(new { message = "Faild to create user" });
        }

        return Ok(new { token = _jwtService.GenerateJwtToken(user.Id.ToString(), "User"), message = "User created successfully" });


    }
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized();
        }

        var token = authHeader.Substring("Bearer ".Length);
        var claimDict = _jwtService.ValidateToken(token);

        if (claimDict == null)  // If token is invalid
        {
            return Unauthorized();
        }

        // Extract the user ID or username from claims (depends on what you store in the token)
        var userId = claimDict[ClaimTypes.NameIdentifier];
        if (!int.TryParse(userId, out int userIdInt))
        {
            return Unauthorized(); // No user identifier in token
        }


        // Fetch user from database
        var user = await _userManager.GetUserById(userIdInt); // Implement this in IUserManager

        if (user == null)
        {
            return NotFound("User not found");
        }

        // Return user profile (you can use a DTO here)
        return Ok(

            user
        );
    }


}


class ResponseModel
{
    public string Token { get; set; }
    public string Message { get; set; }
}