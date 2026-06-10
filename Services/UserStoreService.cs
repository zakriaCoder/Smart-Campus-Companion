using Microsoft.EntityFrameworkCore;
using SmartCampus.Data;
using SmartCampus.Models;

namespace SmartCampus.Services;

public class UserStoreService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    // IMPORTANT FOR FREE HOSTING / DEMO MODE:
    // When SQL Server is not connected online, the app falls back to this shared list.
    // It must be static, otherwise a student created from the Blazor page is lost
    // before the /auth/signin request runs in a new scope.
    private static readonly List<AppUser> SharedFallbackUsers = CreateDefaultUsers();
    private readonly List<AppUser> _fallbackUsers;
    private bool _databaseAvailable = true;

    public UserStoreService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _fallbackUsers = SharedFallbackUsers;
        SeedUsersIfEmpty();
    }

    private AppDbContext CreateDb() => _dbFactory.CreateDbContext();

    private void SeedUsersIfEmpty()
    {
        try
        {
            using var db = CreateDb();

            if (db.AppUsers.Any())
                return;

            db.AppUsers.AddRange(_fallbackUsers.Select(CloneUser));
            db.SaveChanges();
        }
        catch
        {
            _databaseAvailable = false;
        }
    }

    public AppUser? FindByEmail(string email)
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public bool ValidateCredentials(string email, string password)
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers.Any(u =>
                    u.Email.ToLower() == email.ToLower() &&
                    u.Password == password);
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers.Any(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);
    }

    public bool EmailExists(string email)
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers.Any(u => u.Email.ToLower() == email.ToLower());
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public bool IdExists(string id)
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers.Any(u => u.StudentId == id);
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers.Any(u => u.StudentId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AppUser> GetAll()
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers
                    .AsNoTracking()
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.FullName)
                    .ToList();
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(CloneUser)
            .ToList();
    }

    public IReadOnlyList<AppUser> GetByRole(UserRole role)
    {
        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                return db.AppUsers
                    .AsNoTracking()
                    .Where(u => u.Role == role)
                    .OrderBy(u => u.FullName)
                    .ToList();
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        return _fallbackUsers
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .Select(CloneUser)
            .ToList();
    }

    public (bool ok, string error) CreateUser(AppUser user)
    {
        Normalize(user);

        if (EmailExists(user.Email))
            return (false, "Email already registered.");

        if (IdExists(user.StudentId))
            return (false, "ID already in use.");

        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();
                db.AppUsers.Add(user);
                db.SaveChanges();
                return (true, string.Empty);
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        _fallbackUsers.Add(CloneUser(user));
        return (true, string.Empty);
    }

    public (bool ok, string error) UpdateUser(string originalEmail, AppUser updatedUser)
    {
        Normalize(updatedUser);

        if (_databaseAvailable)
        {
            try
            {
                using var db = CreateDb();

                var existing = db.AppUsers.FirstOrDefault(u => u.Email.ToLower() == originalEmail.ToLower());

                if (existing is null)
                    return (false, "Record not found.");

                if (db.AppUsers.Any(u => u.StudentId != existing.StudentId && u.Email.ToLower() == updatedUser.Email.ToLower()))
                    return (false, "Email already registered.");

                if (db.AppUsers.Any(u => u.StudentId != existing.StudentId && u.StudentId == updatedUser.StudentId))
                    return (false, "ID already in use.");

                CopyUser(updatedUser, existing);
                db.SaveChanges();

                return (true, string.Empty);
            }
            catch
            {
                _databaseAvailable = false;
            }
        }

        var fallback = _fallbackUsers.FirstOrDefault(u => u.Email.Equals(originalEmail, StringComparison.OrdinalIgnoreCase));
        if (fallback is null)
            return (false, "Record not found.");

        if (_fallbackUsers.Any(u => !ReferenceEquals(u, fallback) && u.Email.Equals(updatedUser.Email, StringComparison.OrdinalIgnoreCase)))
            return (false, "Email already registered.");

        if (_fallbackUsers.Any(u => !ReferenceEquals(u, fallback) && u.StudentId.Equals(updatedUser.StudentId, StringComparison.OrdinalIgnoreCase)))
            return (false, "ID already in use.");

        CopyUser(updatedUser, fallback);
        return (true, string.Empty);
    }

    public (bool ok, string error) DeleteUser(string email, string currentAdminEmail)
    {
        if (_databaseAvailable)
        {
            try
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
            catch
            {
                _databaseAvailable = false;
            }
        }

        var fallback = _fallbackUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (fallback is null)
            return (false, "Record not found.");

        if (fallback.Email.Equals(currentAdminEmail, StringComparison.OrdinalIgnoreCase))
            return (false, "You cannot delete your own registrar account.");

        if (fallback.Role == UserRole.Admin && _fallbackUsers.Count(u => u.Role == UserRole.Admin) <= 1)
            return (false, "At least one registrar account is required.");

        _fallbackUsers.Remove(fallback);
        return (true, string.Empty);
    }

    public StudentProfile BuildStudentProfile(string email)
    {
        var user = FindByEmail(email) ?? throw new InvalidOperationException("User not found.");
        var semester = user.Designation.Contains('-')
            ? user.Designation.Split('-').Last().Trim()
            : "N/A";

        return new StudentProfile
        {
            FullName = user.FullName,
            StudentId = user.StudentId,
            Email = user.Email,
            Department = user.Department,
            Semester = semester,
            University = "Air University",
            Cgpa = 3.50,
            Tags = new() { user.Department, "Spring 2026" }
        };
    }

    private static void Normalize(AppUser user)
    {
        user.FullName = user.FullName.Trim();
        user.StudentId = user.StudentId.Trim();
        user.Email = user.Email.Trim();
        user.Department = user.Department.Trim();
        user.Designation = user.Designation.Trim();
    }

    private static void CopyUser(AppUser source, AppUser target)
    {
        target.FullName = source.FullName;
        target.StudentId = source.StudentId;
        target.Email = source.Email;
        target.Department = source.Department;
        target.Designation = source.Designation;
        target.Password = source.Password;
        target.Role = source.Role;
    }

    private static AppUser CloneUser(AppUser user) => new()
    {
        FullName = user.FullName,
        StudentId = user.StudentId,
        Email = user.Email,
        Department = user.Department,
        Designation = user.Designation,
        Password = user.Password,
        Role = user.Role
    };

    private static List<AppUser> CreateDefaultUsers() => new()
    {
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
            Designation = "Student - IV-C",
            Password = "zakria123",
            Role = UserRole.Student
        },
        new AppUser
        {
            FullName = "Bilal Khalid",
            StudentId = "241844",
            Email = "241844@students.au.edu.pk",
            Department = "Computer Science",
            Designation = "Student - IV-C",
            Password = "bilal123",
            Role = UserRole.Student
        },
        new AppUser
        {
            FullName = "Ayesha Tariq",
            StudentId = "241900",
            Email = "s241900@students.au.edu.pk",
            Department = "Software Engineering",
            Designation = "Student - II-A",
            Password = "ayesha123",
            Role = UserRole.Student
        },
        new AppUser
        {
            FullName = "Ali Hassan",
            StudentId = "241755",
            Email = "s241755@students.au.edu.pk",
            Department = "Electrical Engineering",
            Designation = "Student - VI-B",
            Password = "ali12345",
            Role = UserRole.Student
        }
    };
}
