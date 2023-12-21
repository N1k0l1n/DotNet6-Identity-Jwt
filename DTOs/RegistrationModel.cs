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
        [DataType(DataType.Password)]
        [RegularExpression(@"^Pa55w0rd!$", ErrorMessage = "Password must be 'Pa55w0rd!' exactly.")]
        public string? Password { get; set; }

        [EmailAddress]
        [Required]
        public string? Email { get; set; }
    }
}
