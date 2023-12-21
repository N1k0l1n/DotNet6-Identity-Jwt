using Microsoft.AspNetCore.Mvc;
using MoviesApisBack.DTOs;

namespace MoviesApisBack.Controllers
{
    [Route("api/[controller]/{action}")]
    [ApiController]
    public class ProtectedController : ControllerBase
    {
        public IActionResult GetData()
        {
            var status = new Status();
            status.StatusCode = 1;
            status.Message = "Data from protected controller";
            return Ok(status);
        }
    }
}
