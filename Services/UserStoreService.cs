using Microsoft.EntityFrameworkCore;
using SmartCampus.Data;
using SmartCampus.Models;

namespace SmartCampus.Services;

public class UserStoreService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserStoreService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        SeedUsersIfEmpty();
    }

    private AppDbContext CreateDb() => _dbFactory.CreateDbContext();

    private void SeedUsersIfEmpty()
    {
        using var db = CreateDb();

        if (db.AppUsers.Any())
            return;

        db.AppUsers.AddRange(
            new AppUser
            {
                FullName = "Registrar Officer",
                StudentId = "ADM001",
                Email = "admin@au.edu.pk",
                Department = "Administration",
                Designation = "Academic Registration Officer",
                Password = "admin123",
                Role = UserRole.Admin
            },
            new AppUser
            {
                FullName = "Dr. Kamran Malik",
                StudentId = "HOD001",
                Email = "hod.cs@au.edu.pk",
                Department = "Computer Science",
                Designation = "Head of Department",
                Password = "hod12345",
                Role = UserRole.HOD
            },
            new AppUser
            {
                FullName = "Dr. Sara Ahmed",
                StudentId = "HOD002",
                Email = "hod.se@au.edu.pk",
                Department = "Software Engineering",
                Designation = "Head of Department",
                Password = "hod12345",
                Role = UserRole.HOD
            },
            new AppUser
            {
                FullName = "Mr. Bilal Raza",
                StudentId = "FAC001",
                Email = "bilal.raza@au.edu.pk",
                Department = "Computer Science",
                Designation = "Lecturer",
                Password = "faculty123",
                Role = UserRole.Faculty
            },
            new AppUser
            {
                FullName = "Ms. Nadia Khan",
                StudentId = "FAC002",
                Email = "nadia.khan@au.edu.pk",
                Department = "Software Engineering",
                Designation = "Senior Lecturer / Advisor",
                Password = "faculty123",
                Role = UserRole.Faculty
            },
            new AppUser
            {
                FullName = "Muhammad Zakria",
                StudentId = "241856",
                Email = "s241856@students.au.edu.pk",
                Department = "Computer Science",
                Designation = "Student — IV-C",
                Password = "zakria123",
                Role = UserRole.Student
            },
            new AppUser
            {
                FullName = "Bilal Khalid",
                StudentId = "241844",
                Email = "241844@students.au.edu.pk",
                Department = "Computer Science",
                Designation = "Student — IV-C",
                Password = "bilal123",
                Role = UserRole.Student
            },
            new AppUser
            {
                FullName = "Ayesha Tariq",
                StudentId = "241900",
                Email = "s241900@students.au.edu.pk",
                Department = "Software Engineering",
                Designation = "Student — II-A",
                Password = "ayesha123",
                Role = UserRole.Student
            },
            new AppUser
            {
                FullName = "Ali Hassan",
                StudentId = "241755",
                Email = "s241755@students.au.edu.pk",
                Department = "Electrical Engineering",
                Designation = "Student — VI-B",
                Password = "ali12345",
                Role = UserRole.Student
            }
        );

        db.SaveChanges();
    }

    public AppUser? FindByEmail(string email)
    {
        using var db = CreateDb();
        return db.AppUsers
            .AsNoTracking()
            .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
    }

    public bool ValidateCredentials(string email, string password)
    {
        using var db = CreateDb();
        return db.AppUsers.Any(u =>
            u.Email.ToLower() == email.ToLower() &&
            u.Password == password);
    }

    public bool EmailExists(string email)
    {
        using var db = CreateDb();
        return db.AppUsers.Any(u => u.Email.ToLower() == email.ToLower());
    }

    public bool IdExists(string id)
    {
        using var db = CreateDb();
        return db.AppUsers.Any(u => u.StudentId == id);
    }

    public IReadOnlyList<AppUser> GetAll()
    {
        using var db = CreateDb();
        return db.AppUsers
            .AsNoTracking()
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToList();
    }

    public IReadOnlyList<AppUser> GetByRole(UserRole role)
    {
        using var db = CreateDb();
        return db.AppUsers
            .AsNoTracking()
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToList();
    }

    public (bool ok, string error) CreateUser(AppUser user)
    {
        using var db = CreateDb();

        user.FullName = user.FullName.Trim();
        user.StudentId = user.StudentId.Trim();
        user.Email = user.Email.Trim();
        user.Department = user.Department.Trim();
        user.Designation = user.Designation.Trim();

        if (db.AppUsers.Any(u => u.Email.ToLower() == user.Email.ToLower()))
            return (false, "Email already registered.");

        if (db.AppUsers.Any(u => u.StudentId == user.StudentId))
            return (false, "ID already in use.");

        db.AppUsers.Add(user);
        db.SaveChanges();

        return (true, string.Empty);
    }

    public (bool ok, string error) UpdateUser(string originalEmail, AppUser updatedUser)
    {
        using var db = CreateDb();

        var existing = db.AppUsers.FirstOrDefault(u => u.Email.ToLower() == originalEmail.ToLower());

        if (existing is null)
            return (false, "Record not found.");

        var newEmail = updatedUser.Email.Trim();
        var newId = updatedUser.StudentId.Trim();

        if (db.AppUsers.Any(u => u.StudentId != existing.StudentId && u.Email.ToLower() == newEmail.ToLower()))
            return (false, "Email already registered.");

        if (db.AppUsers.Any(u => u.StudentId != existing.StudentId && u.StudentId == newId))
            return (false, "ID already in use.");

        existing.FullName = updatedUser.FullName.Trim();
        existing.StudentId = newId;
        existing.Email = newEmail;
        existing.Department = updatedUser.Department.Trim();
        existing.Designation = updatedUser.Designation.Trim();
        existing.Role = updatedUser.Role;

        db.SaveChanges();

        return (true, string.Empty);
    }

    public (bool ok, string error) DeleteUser(string email, string currentAdminEmail)
    {
        using var db = CreateDb();

        var user = db.AppUsers.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

        if (user is null)
            return (false, "Record not found.");

        if (user.Email.Equals(currentAdminEmail, StringComparison.OrdinalIgnoreCase))
            return (false, "You cannot delete your own registrar account.");

        if (user.Role == UserRole.Admin && db.AppUsers.Count(u => u.Role == UserRole.Admin) <= 1)
            return (false, "At least one registrar account is required.");

        db.AppUsers.Remove(user);
        db.SaveChanges();

        return (true, string.Empty);
    }

    public StudentProfile BuildStudentProfile(string email)
    {
        using var db = CreateDb();

        var u = db.AppUsers
            .AsNoTracking()
            .FirstOrDefault(x => x.Email.ToLower() == email.ToLower())
            ?? throw new InvalidOperationException("User not found.");

        var semester = u.Designation.Contains("—")
            ? u.Designation.Split("—").Last().Trim()
            : "N/A";

        return new StudentProfile
        {
            FullName = u.FullName,
            StudentId = u.StudentId,
            Email = u.Email,
            Department = u.Department,
            Semester = semester,
            University = "Air University",
            Cgpa = 3.50,
            Tags = new() { u.Department, "Spring 2026" }
        };
    }
}