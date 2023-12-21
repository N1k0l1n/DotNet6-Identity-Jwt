using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApisBack.Data;
using MoviesApisBack.DTOs;
using MoviesApisBack.Repositories.Abstract;

namespace MoviesApisBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly DatabaseContext _ctx;
        private readonly ITokenService _service;
        public TokenController(DatabaseContext ctx, ITokenService service)
        {
            _ctx = ctx;
            _service = service;

        }

        [HttpPost("RefreshToken")]
        public IActionResult Refresh(RefreshTokenRequest tokenApiModel)
        {
            if (tokenApiModel is null)
                return BadRequest("Invalid client request");

            // Retrieving access and refresh tokens from the request model
            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;

            // Extracting user information from an expired access token
            var principal = _service.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity?.Name;

            // Finding the user in the database based on the username
            var user = _ctx.TokenInfo.SingleOrDefault(u => u.Username == username);

            // Handling various conditions for invalid requests or tokens
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.Now)
                return BadRequest("Invalid client request");

            // Generating new access and refresh tokens
            var newAccessToken = _service.GetToken(principal.Claims);
            var newRefreshToken = _service.GetRefreshToken();

            // Updating the user's refresh token in the database
            user.RefreshToken = newRefreshToken;
            _ctx.SaveChanges();

            // Returning a new set of tokens in a response model
            return Ok(new RefreshTokenRequest()
            {
                AccessToken = newAccessToken.TokenString,
                RefreshToken = newRefreshToken
            });
        }

        // Endpoint for revoking tokens (requiring authorization)
        [HttpPost("Revoke")]
        [Authorize]
        public IActionResult Revoke()
        {
            try
            {
                // Retrieving the username of the authenticated user
                var username = User.Identity?.Name;
                // Finding the user in the database based on the username
                var user = _ctx.TokenInfo.SingleOrDefault(u => u.Username == username);

                // Returning a bad request if the user is not found
                if (user is null)
                    return BadRequest();

                // Removing the refresh token for the user in the database
                user.RefreshToken = null;
                _ctx.SaveChanges();

                return Ok(true);
            }
            catch
            {
                // For security reasons, return a generic bad request without exposing detailed exception messages
                return BadRequest("An error occurred while processing the request. Please try again later.");
            }
        }
    }
}