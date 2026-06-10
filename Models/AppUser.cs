namespace SmartCampus.Models;

public class AppUser
{
    public string FullName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;

    public string Initials => string.Concat(
        FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w[0])).ToUpper();
}