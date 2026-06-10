namespace SmartCampus.Services;

public class FacultyAcademicService
{
    private readonly List<FacultyCourseRecord> _courses = new()
    {
        new("Computer Science", "CS-284L", "Visual Programming Lab", "IV-C", 1, 28, 5),
        new("Computer Science", "CS-301", "Database Systems", "V-B", 3, 32, 4),
        new("Computer Science", "CS-411", "Web Engineering", "IV-B", 3, 25, 3),

        new("Software Engineering", "SE-201", "Software Requirements Engineering", "IV-A", 3, 36, 4),
        new("Software Engineering", "SE-305", "Software Quality Engineering", "VI-A", 3, 34, 3)
    };

    private readonly List<FacultyResourceRecord> _resources = new()
    {
        new("Computer Science", "CS-284L", "Blazor CRUD Notes", "PDF", "Lecture notes for CRUD operations", "18-May-2026"),
        new("Computer Science", "CS-301", "Database ERD Examples", "PDF", "ERD examples for practice", "18-May-2026"),
        new("Software Engineering", "SE-201", "SRS Template", "DOCX", "Requirement document template", "18-May-2026")
    };

    private readonly List<FacultyAdviseeRecord> _advisees = new()
    {
        new("Computer Science", "Muhammad Zakria", "241856", "IV-C", "Good", "Course Registration", "Pending"),
        new("Computer Science", "Ali Hassan", "241755", "VI-B", "Needs Attention", "Clearance", "Pending"),
        new("Software Engineering", "Ayesha Tariq", "241900", "II-A", "Good", "Advising Meeting", "Completed")
    };

    public IReadOnlyList<FacultyCourseRecord> GetCourses(string department)
        => _courses
            .Where(c => c.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public IReadOnlyList<FacultyResourceRecord> GetResources(string department)
        => _resources
            .Where(r => r.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Date)
            .ToList();

    public IReadOnlyList<FacultyAdviseeRecord> GetAdvisees(string department)
        => _advisees
            .Where(a => a.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public void AddResource(string department, string courseCode, string title, string type, string description)
    {
        _resources.Insert(0, new FacultyResourceRecord(
            department,
            courseCode,
            title,
            type,
            description,
            DateTime.Now.ToString("dd-MMM-yyyy")));
    }

    public bool DeleteResource(string title)
    {
        var resource = _resources.FirstOrDefault(r => r.Title == title);
        if (resource is null)
            return false;

        _resources.Remove(resource);
        return true;
    }

    public FacultyReportSnapshot GetReport(string department)
    {
        var courses = GetCourses(department);
        var resources = GetResources(department);
        var advisees = GetAdvisees(department);

        var assignments = courses.Sum(c => c.Assignments);
        var students = courses.Sum(c => c.StudentCount);

        return new FacultyReportSnapshot(
            CourseCount: courses.Count,
            StudentCount: students,
            AssignmentCount: assignments,
            ResourceCount: resources.Count,
            AdviseeCount: advisees.Count,
            PendingRequests: advisees.Count(a => a.RequestStatus == "Pending"),
            AtRiskStudents: advisees.Count(a => a.AcademicStatus == "Needs Attention"));
    }
}

public sealed record FacultyCourseRecord(
    string Department,
    string Code,
    string Title,
    string Section,
    int CreditHours,
    int StudentCount,
    int Assignments);

public sealed record FacultyResourceRecord(
    string Department,
    string CourseCode,
    string Title,
    string Type,
    string Description,
    string Date);

public sealed record FacultyAdviseeRecord(
    string Department,
    string FullName,
    string StudentId,
    string Semester,
    string AcademicStatus,
    string RequestType,
    string RequestStatus);

public sealed record FacultyReportSnapshot(
    int CourseCount,
    int StudentCount,
    int AssignmentCount,
    int ResourceCount,
    int AdviseeCount,
    int PendingRequests,
    int AtRiskStudents);
