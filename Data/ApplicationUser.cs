using Microsoft.AspNetCore.Identity;

namespace MoviesApisBack.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
    }
}
