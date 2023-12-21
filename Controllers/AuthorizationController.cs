using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoviesApisBack.Data;
using MoviesApisBack.DTOs;
using MoviesApisBack.Models;
using MoviesApisBack.Repositories.Abstract;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MoviesApisBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ITokenService _tokenService;
        public AuthorizationController(DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService
            )
        {
            this._context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._tokenService = tokenService;
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            var status = new Status();
            // check validations
            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.Message = "please pass all the valid fields";
                return Ok(status);
            }
            // lets find the user
            var user = await userManager.FindByNameAsync(model.Username);
            if (user is null)
            {
                status.StatusCode = 0;
                status.Message = "invalid username";
                return Ok(status);
            }
            // check current password
            if (!await userManager.CheckPasswordAsync(user, model.CurrentPassword))
            {
                status.StatusCode = 0;
                status.Message = "invalid current password";
                return Ok(status);
            }

            // change password here
            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Failed to change password";
                return Ok(status);
            }
            status.StatusCode = 1;
            status.Message = "Password has changed successfully";
            return Ok(result);
        }

        [HttpPost("Login")]

        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Finding the user by username using UserManager
            var user = await userManager.FindByNameAsync(model.Username);

            // Checking if the user exists and if the password provided matches the user's password
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                // Retrieving roles associated with the user
                var userRoles = await userManager.GetRolesAsync(user);

                // Creating a list of claims for authentication
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),  // Claim for username
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique identifier claim
                };

                // Adding user roles as claims
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                // Generating a JWT token and a refresh token using a token service
                var token = _tokenService.GetToken(authClaims);
                var refreshToken = _tokenService.GetRefreshToken();

                // Checking if token information exists for the user
                var tokenInfo = _context.TokenInfo.FirstOrDefault(a => a.Username == user.UserName);

                // If token information doesn't exist, create a new entry
                if (tokenInfo == null)
                {
                    var info = new TokenInfo
                    {
                        Username = user.UserName,
                        RefreshToken = refreshToken,
                        RefreshTokenExpiry = DateTime.Now.AddDays(1)  // Setting refresh token expiry
                    };
                    _context.TokenInfo.Add(info);
                }
                // If token information exists, update the existing entry
                else
                {
                    tokenInfo.RefreshToken = refreshToken;
                    tokenInfo.RefreshTokenExpiry = DateTime.Now.AddDays(1);
                }
                try
                {
                    // Saving changes to the database (adding or updating token information)
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }

                // Returning a successful login response with user information and tokens

                return Ok(new LoginResponse
                {
                    Name = user.Name,
                    Username = user.UserName,
                    Token = token.TokenString,
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo,
                    StatusCode = 1,
                    Message = "Logged in"
                });

            }

            // Login failed condition: Invalid username or password

            return Ok(
                new LoginResponse
                {
                    StatusCode = 0,
                    Message = "Invalid Username or Password",
                    Token = "",
                    Expiration = null
                });
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// {
        ///     "name": "John Doe",
        ///     "username": "johndoe",
        ///     "password": "StrongP@ss123",
        ///     "email": "johndoe@example.com"
        /// }
        /// </remarks>
        /// <param name="model">Registration information</param>
        /// <response code="200">Returns status indicating successful registration</response>
        /// <response code="400">If the request is invalid or registration fails</response>
        [HttpPost("Registration")]
        public async Task<IActionResult> Registration([FromBody] RegistrationModel model)
        {
            var status = new Status();
            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.Message = "Please pass all the required fields";
                return Ok(status);
            }
            // check if user exists
            var userExists = await userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                status.StatusCode = 0;
                status.Message = "Invalid username";
                return Ok(status);
            }
            var user = new ApplicationUser
            {
                UserName = model.Username,
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = model.Email,
                Name = model.Name
            };
            // create a user here
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "User creation failed";
                return Ok(status);
            }

            // add roles here
            // for admin registration UserRoles.Admin instead of UserRoles.Roles
            if (!await roleManager.RoleExistsAsync(UserRoles.User))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

            if (await roleManager.RoleExistsAsync(UserRoles.User))
            {
                await userManager.AddToRoleAsync(user, UserRoles.User);
            }
            status.StatusCode = 1;
            status.Message = "Sucessfully registered";
            return Ok(status);

        }

        // after registering admin we will comment this code, because i want only one admin in this application
        //[HttpPost]
        //public async Task<IActionResult> RegistrationAdmin([FromBody] RegistrationModel model)
        //{
        //    var status = new Status();
        //    if (!ModelState.IsValid)
        //    {
        //        status.StatusCode = 0;
        //        status.Message = "Please pass all the required fields";
        //        return Ok(status);
        //    }
        //    // check if user exists
        //    var userExists = await userManager.FindByNameAsync(model.Username);
        //    if (userExists != null)
        //    {
        //        status.StatusCode = 0;
        //        status.Message = "Invalid username";
        //        return Ok(status);
        //    }
        //    var user = new ApplicationUser
        //    {
        //        UserName = model.Username,
        //        SecurityStamp = Guid.NewGuid().ToString(),
        //        Email = model.Email,
        //        Name = model.Name
        //    };
        //    // create a user here
        //    var result = await userManager.CreateAsync(user, model.Password);
        //    if (!result.Succeeded)
        //    {
        //        status.StatusCode = 0;
        //        status.Message = "User creation failed";
        //        return Ok(status);
        //    }

        //    // add roles here
        //    // for admin registration UserRoles.Admin instead of UserRoles.Roles
        //    if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
        //        await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));

        //    if (await roleManager.RoleExistsAsync(UserRoles.Admin))
        //    {
        //        await userManager.AddToRoleAsync(user, UserRoles.Admin);
        //    }
        //    status.StatusCode = 1;
        //    status.Message = "Sucessfully registered";
        //    return Ok(status);

        //}

    }
}