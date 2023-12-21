using System.ComponentModel.DataAnnotations;

namespace MoviesApisBack.DTOs
{
    public class LoginModel
    {
        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }

    }
}
