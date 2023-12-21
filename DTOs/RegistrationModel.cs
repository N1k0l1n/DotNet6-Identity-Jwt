using System.ComponentModel.DataAnnotations;

namespace MoviesApisBack.DTOs
{
    public class RegistrationModel
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }

        [EmailAddress]
        [Required]
        public string? Email { get; set; }
    }
}
