using System.ComponentModel.DataAnnotations;

namespace SmartCampus.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Student email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}