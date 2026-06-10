namespace SmartCampus.Services;

public class CourseRegistrationRequestService
{
    private readonly List<CourseRegistrationRequest> _requests = new()
    {
        new()
        {
            Id = 1,
            StudentName = "Muhammad Zakria",
            StudentId = "241856",
            Department = "Computer Science",
            Semester = "IV-C",
            CourseCode = "CS-401",
            CourseTitle = "Artificial Intelligence",
            RequestType = "Additional Course",
            StudentReason = "I want to improve my credit hours this semester.",
            Status = "Pending Advisor Review",
            AdvisorRemarks = "",
            HodRemarks = "",
            CreatedAt = "18-May-2026"
        },
        new()
        {
            Id = 2,
            StudentName = "Ali Hassan",
            StudentId = "241755",
            Department = "Computer Science",
            Semester = "VI-B",
            CourseCode = "CS-301",
            CourseTitle = "Database Systems",
            RequestType = "Course Registration",
            StudentReason = "Course is required for my study plan.",
            Status = "Forwarded to HOD",
            AdvisorRemarks = "Student is eligible. Recommended for approval.",
            HodRemarks = "",
            CreatedAt = "18-May-2026"
        }
    };

    private int _nextId = 3;

    public bool IsRegistrationOpen { get; private set; } = true;

    public IReadOnlyList<CourseRegistrationRequest> GetByDepartment(string department)
        => _requests
            .Where(r => r.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Id)
            .ToList();

    public IReadOnlyList<CourseRegistrationRequest> GetByStudent(string studentId)
        => _requests
            .Where(r => r.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Id)
            .ToList();

    public IReadOnlyList<CourseRegistrationRequest> GetForAdvisor(string department)
        => GetByDepartment(department)
            .Where(r => r.Status == "Pending Advisor Review" || r.Status == "Forwarded to HOD")
            .ToList();

    public IReadOnlyList<CourseRegistrationRequest> GetForHod(string department)
        => GetByDepartment(department)
            .Where(r => r.Status == "Forwarded to HOD")
            .ToList();

    public void CreateStudentRequest(
        string studentName,
        string studentId,
        string department,
        string semester,
        string courseCode,
        string courseTitle,
        string requestType,
        string reason)
    {
        if (!IsRegistrationOpen)
            return;

        _requests.Insert(0, new CourseRegistrationRequest
        {
            Id = _nextId++,
            StudentName = studentName.Trim(),
            StudentId = studentId.Trim(),
            Department = department.Trim(),
            Semester = semester.Trim(),
            CourseCode = courseCode.Trim(),
            CourseTitle = courseTitle.Trim(),
            RequestType = requestType.Trim(),
            StudentReason = reason.Trim(),
            Status = "Pending Advisor Review",
            AdvisorRemarks = "",
            HodRemarks = "",
            CreatedAt = DateTime.Now.ToString("dd-MMM-yyyy")
        });
    }

    public bool DeleteStudentDraft(int id, string studentId)
    {
        var request = _requests.FirstOrDefault(r =>
            r.Id == id &&
            r.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase) &&
            r.Status == "Pending Advisor Review");

        if (request is null)
            return false;

        _requests.Remove(request);
        return true;
    }

    public void ForwardToHod(int id, string advisorRemarks)
    {
        var request = _requests.FirstOrDefault(r => r.Id == id);
        if (request is null)
            return;

        request.AdvisorRemarks = advisorRemarks.Trim();
        request.Status = "Forwarded to HOD";
    }

    public void HodDecision(int id, string decision, string hodRemarks)
    {
        var request = _requests.FirstOrDefault(r => r.Id == id);
        if (request is null)
            return;

        request.Status = decision;
        request.HodRemarks = hodRemarks.Trim();
    }
}

public class CourseRegistrationRequest
{
    public int Id { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Department { get; set; } = "";
    public string Semester { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string RequestType { get; set; } = "";
    public string StudentReason { get; set; } = "";
    public string Status { get; set; } = "";
    public string AdvisorRemarks { get; set; } = "";
    public string HodRemarks { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}
