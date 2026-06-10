using Microsoft.EntityFrameworkCore;
using SmartCampus.Data;
namespace SmartCampus.Services;

public sealed class CampusRealtimeService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public event Action? OnChange;

    public StudentProfileVm Profile { get; } = new()
    {
        FullName = "Bilal Khalid",
        StudentId = "241844",
        Email = "241844@students.au.edu.pk",
        Department = "Computer Science",
        Semester = "IV-C",
        Cgpa = 3.72,
        Phone = "+92 300 0000000",
        Address = "Air University Boys Hostel / Islamabad",
        Bio = "BSCS student working on Smart Campus Companion mini FYP.",
        Skills = "Blazor, C#, SQL Server, UI/UX, Visual Programming"
    };

    public List<PortalStudent> Students { get; } = new();
    public List<PortalFaculty> FacultyMembers { get; } = new();
    public List<PortalCourse> Courses { get; } = new();
    public List<CourseEnrollment> Enrollments { get; } = new();
    public List<PortalAssignment> CourseAssignments { get; } = new();
    public List<PortalSubmission> Submissions { get; } = new();
    public List<PortalAnnouncement> Announcements { get; } = new();
    public List<AttendanceRecord> AttendanceRecords { get; } = new();
    public List<ProfileCorrectionRequest> ProfileCorrectionRequests { get; } = new();
    public List<DocumentRequest> DocumentRequests { get; } = new();
    public List<ClassroomPost> ClassroomPosts { get; } = new();

    public List<CampusAssignment> Assignments { get; } = new();
    public List<CampusSchedule> Schedule { get; } = new();
    public List<LostFoundItem> LostFound { get; } = new();
    public List<CampusEvent> Events { get; } = new();
    public List<MarketplaceItem> Marketplace { get; } = new();
    public List<RideOffer> Rides { get; } = new();
    public List<StudyResource> Resources { get; } = new();
    public List<CampusNotification> Notifications { get; } = new();
    public List<CampusActivity> Activities { get; } = new();

    public CampusRealtimeService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        LoadFromDatabaseOrSeed();
    }

    public int PendingAssignments => Assignments.Count(a => !a.IsCompleted && a.DueDate.Date >= DateTime.Today) + CourseAssignments.Count(a => a.DueDate.Date >= DateTime.Today);
    public int OverdueAssignments => Assignments.Count(a => !a.IsCompleted && a.DueDate.Date < DateTime.Today) + CourseAssignments.Count(a => a.DueDate.Date < DateTime.Today);
    public int CompletedAssignments => Assignments.Count(a => a.IsCompleted) + Submissions.Count(s => s.Status == "Submitted" || s.Status == "Checked");
    public int TodayClasses => Schedule.Count(s => s.Day == DateTime.Today.DayOfWeek.ToString()) + Courses.Count(c => c.Day == DateTime.Today.DayOfWeek.ToString());
    public int UnreadNotifications => Notifications.Count(n => !n.IsRead);
    public int ActiveListings => Marketplace.Count(m => m.Status == "Available");
    public int OpenLostFound => LostFound.Count(l => l.Status == "Open");
    public int RegisteredEvents => Events.Count(e => e.IsRegistered);
    public int AvailableRides => Rides.Count(r => r.SeatsLeft > 0);
    public int ResourceDownloads => Resources.Sum(r => r.Downloads);
    public int ActiveCourses => Courses.Count(c => c.IsOpenForEnrollment);
    public int TotalEnrollments => Enrollments.Count(e => e.Status == "Enrolled");
    public int PendingSubmissions => CourseAssignments.Count - Submissions.Select(s => s.AssignmentId).Distinct().Count();
    public int PendingProfileRequests => ProfileCorrectionRequests.Count(r => r.Status == "Pending");
    public int PendingDocumentRequests => DocumentRequests.Count(r => r.Status == "Pending");
    public int AttendanceMarkedToday => AttendanceRecords.Count(r => r.Date.Date == DateTime.Today);

    public IEnumerable<CampusActivity> RecentActivities => Activities.OrderByDescending(a => a.CreatedAt).Take(14);
    public IEnumerable<CampusNotification> LatestNotifications => Notifications.OrderByDescending(n => n.CreatedAt).Take(6);
    public IEnumerable<PortalAnnouncement> LatestAnnouncements => Announcements.OrderByDescending(a => a.CreatedAt).Take(8);

    public PortalStudent? FindStudentByEmail(string email)
        => Students.FirstOrDefault(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public PortalFaculty? FindFacultyByEmail(string email)
        => FacultyMembers.FirstOrDefault(f => f.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public PortalStudent? FindStudentById(string id)
        => Students.FirstOrDefault(s => s.StudentId.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<PortalCourse> GetStudentCourses(string email)
        => Enrollments.Where(e => e.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase) && e.Status == "Enrolled")
            .Join(Courses, e => e.CourseId, c => c.Id, (e, c) => c)
            .OrderBy(c => c.Code);

    public IEnumerable<PortalCourse> GetAvailableCourses(string email)
        => Courses.Where(c => c.IsOpenForEnrollment && !IsStudentEnrolled(email, c.Id)).OrderBy(c => c.Code);

    public IEnumerable<PortalCourse> GetCoursesForFaculty(string email)
        => Courses.Where(c => c.FacultyEmail.Equals(email, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Code);

    public IEnumerable<PortalStudent> GetStudentsForCourse(string courseId)
        => Enrollments.Where(e => e.CourseId == courseId && e.Status == "Enrolled")
            .Join(Students, e => e.StudentEmail, s => s.Email, (e, s) => s)
            .OrderBy(s => s.FullName);

    public IEnumerable<PortalAnnouncement> GetAnnouncementsForStudent(string email)
    {
        var courseIds = GetStudentCourses(email).Select(c => c.Id).ToHashSet();
        return Announcements
            .Where(a => a.Audience == "All" || a.Audience == "Students" || (a.Audience == "Course" && a.CourseId is not null && courseIds.Contains(a.CourseId)))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt);
    }

    public IEnumerable<PortalAssignment> GetAssignmentsForStudent(string email)
    {
        var courseIds = GetStudentCourses(email).Select(c => c.Id).ToHashSet();
        return CourseAssignments.Where(a => courseIds.Contains(a.CourseId)).OrderBy(a => a.DueDate);
    }

    public PortalSubmission? GetSubmission(string assignmentId, string studentEmail)
        => Submissions.FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

    public bool IsStudentEnrolled(string email, string courseId)
        => Enrollments.Any(e => e.CourseId == courseId && e.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase) && e.Status == "Enrolled");

    public int EnrollmentCount(string courseId)
        => Enrollments.Count(e => e.CourseId == courseId && e.Status == "Enrolled");

    public void EnrollStudent(string studentEmail, string courseId)
    {
        var student = FindStudentByEmail(studentEmail);
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        if (student is null || course is null) return;
        if (student.RecordStatus != "Active") return;
        if (IsStudentEnrolled(studentEmail, courseId)) return;
        if (!course.IsOpenForEnrollment) return;
        if (EnrollmentCount(courseId) >= course.Capacity) return;

        Enrollments.Insert(0, new CourseEnrollment
        {
            Id = NewId(),
            CourseId = course.Id,
            StudentEmail = student.Email,
            StudentId = student.StudentId,
            StudentName = student.FullName,
            EnrolledOn = DateTime.Now,
            Status = "Enrolled"
        });

        PublishClassroomPost(course.Id, "New student joined", $"{student.FullName} joined {course.Code} through course registration.", "System", "SmartCampus");
        AddActivity("Course enrolled", $"{student.FullName} enrolled in {course.Code}", "Registration", "bi-journal-plus", "success");
        AddNotification("Course Registration", $"You are enrolled in {course.Code} - {course.Title}.", "Registration", "success");
        RaiseChanged();
    }

    public void DropCourse(string studentEmail, string courseId)
    {
        var enrollment = Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase) && e.Status == "Enrolled");
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        if (enrollment is null || course is null) return;
        enrollment.Status = "Dropped";
        AddActivity("Course dropped", $"{enrollment.StudentName} dropped {course.Code}", "Registration", "bi-journal-minus", "warning");
        AddNotification("Course Dropped", $"{course.Code} was removed from your enrolled courses.", "Registration", "warning");
        RaiseChanged();
    }

    // HOD work: create course offerings, assign faculty, set timings, open/close registration.
    public void CreateCourse(string code, string title, string department, string semester, int creditHours, int capacity)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title)) return;
        if (Courses.Any(c => c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase))) return;
        var cleanCode = code.Trim().ToUpperInvariant();
        Courses.Insert(0, new PortalCourse
        {
            Id = NewId(),
            Code = cleanCode,
            Title = title.Trim(),
            Department = Clean(department, "Computer Science"),
            Semester = Clean(semester, "IV-C"),
            CreditHours = Math.Max(1, creditHours),
            Capacity = Math.Max(10, capacity),
            FacultyName = "Not Assigned",
            FacultyEmail = "",
            Day = "Monday",
            StartTime = "09:00",
            EndTime = "10:30",
            Room = "TBA",
            IsOpenForEnrollment = true,
            ClassroomCode = $"AU-{cleanCode.Replace("-", "")}-26",
            ClassroomLink = "https://classroom.google.com/"
        });
        AddActivity("Course offering created", $"HOD created {cleanCode} - {title.Trim()}", "HOD", "bi-journal-plus", "primary");
        AddNotification("New Course Offering", $"{cleanCode} is now available for registration.", "HOD", "info");
        RaiseChanged();
    }

    public void AssignFacultyToCourse(string courseId, string facultyEmail, string day, string startTime, string endTime, string room)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        var faculty = FacultyMembers.FirstOrDefault(f => f.Email.Equals(facultyEmail, StringComparison.OrdinalIgnoreCase));
        if (course is null || faculty is null) return;
        course.FacultyEmail = faculty.Email;
        course.FacultyName = faculty.FullName;
        course.Day = Clean(day, course.Day);
        course.StartTime = Clean(startTime, course.StartTime);
        course.EndTime = Clean(endTime, course.EndTime);
        course.Room = Clean(room, course.Room);
        AddActivity("Faculty assigned", $"{faculty.FullName} assigned to {course.Code}", "HOD", "bi-person-check", "success");
        AddNotification("Course Allocation Updated", $"{course.Code} is now assigned to {faculty.FullName}.", "HOD", "success");
        RaiseChanged();
    }

    public void ToggleCourseEnrollment(string courseId)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return;
        course.IsOpenForEnrollment = !course.IsOpenForEnrollment;
        var state = course.IsOpenForEnrollment ? "opened" : "closed";
        AddActivity("Registration window updated", $"{course.Code} registration {state} by HOD", "HOD", "bi-toggle-on", course.IsOpenForEnrollment ? "success" : "warning");
        AddNotification("Registration Updated", $"{course.Code} registration is now {state}.", "Course Registration", course.IsOpenForEnrollment ? "success" : "warning");
        RaiseChanged();
    }

    public void PostAnnouncement(string title, string message, string audience, string courseId, string postedBy, string role)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        var finalAudience = audience == "Course" ? "Course" : audience;
        var item = new PortalAnnouncement
        {
            Id = NewId(),
            Title = Clean(title, "Important Campus Update"),
            Message = Clean(message, "Please check your portal for details."),
            Audience = finalAudience,
            CourseId = finalAudience == "Course" ? course?.Id : null,
            CourseCode = finalAudience == "Course" ? course?.Code ?? "Course" : finalAudience,
            PostedByName = Clean(postedBy, role),
            PostedByRole = role,
            CreatedAt = DateTime.Now,
            Tone = role == "Registrar" ? "danger" : role == "HOD" ? "primary" : "success",
            IsPinned = role == "Registrar" || role == "HOD"
        };
        Announcements.Insert(0, item);

        if (finalAudience == "Course" && course is not null)
        {
            PublishClassroomPost(course.Id, item.Title, item.Message, "Announcement", item.PostedByName, false);
        }

        AddActivity("Announcement posted", $"{role} posted: {item.Title}", "Announcements", "bi-megaphone", item.Tone);
        AddNotification(item.Title, item.Message, "Announcements", item.Tone);
        RaiseChanged();
    }

    public void DeleteAnnouncement(string id)
    {
        var item = Announcements.FirstOrDefault(a => a.Id == id);
        if (item is null) return;
        Announcements.Remove(item);
        AddActivity("Announcement removed", item.Title, "Announcements", "bi-trash", "danger");
        RaiseChanged();
    }

    public void CreateCourseAssignment(string courseId, string title, string description, DateTime dueDate, int marks, string teacherEmail)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return;
        var faculty = FindFacultyByEmail(teacherEmail);
        var item = new PortalAssignment
        {
            Id = NewId(),
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseTitle = course.Title,
            Title = Clean(title, "New Assignment"),
            Description = Clean(description, "Assignment details are available in the course classroom."),
            DueDate = dueDate,
            Marks = Math.Max(1, marks),
            TeacherEmail = teacherEmail,
            TeacherName = faculty?.FullName ?? course.FacultyName,
            CreatedAt = DateTime.Now
        };
        CourseAssignments.Insert(0, item);
        PublishClassroomPost(course.Id, item.Title, $"Assignment posted. Due: {item.DueDate:MMM dd}. Total marks: {item.Marks}. {item.Description}", "Assignment", item.TeacherName, false);
        AddActivity("Assignment posted", $"{item.CourseCode}: {item.Title}", "Assignments", "bi-journal-check", "success");
        AddNotification("New Assignment", $"{item.CourseCode}: {item.Title} due {item.DueDate:MMM dd}.", "Assignments", "warning");
        RaiseChanged();
    }

    public void DeleteCourseAssignment(string id)
    {
        var item = CourseAssignments.FirstOrDefault(a => a.Id == id);
        if (item is null) return;
        CourseAssignments.Remove(item);
        Submissions.RemoveAll(s => s.AssignmentId == id);
        AddActivity("Assignment removed", item.Title, "Assignments", "bi-trash", "danger");
        RaiseChanged();
    }

    public void SubmitCourseAssignment(string assignmentId, string studentEmail, string fileName, string remarks)
    {
        var assignment = CourseAssignments.FirstOrDefault(a => a.Id == assignmentId);
        var student = FindStudentByEmail(studentEmail);
        if (assignment is null || student is null) return;
        var existing = GetSubmission(assignmentId, studentEmail);
        if (existing is null)
        {
            Submissions.Insert(0, new PortalSubmission
            {
                Id = NewId(),
                AssignmentId = assignment.Id,
                CourseId = assignment.CourseId,
                CourseCode = assignment.CourseCode,
                StudentEmail = student.Email,
                StudentName = student.FullName,
                SubmittedOn = DateTime.Now,
                FileName = Clean(fileName, "submission.pdf"),
                Remarks = Clean(remarks, "Submitted from SmartCampus portal"),
                Status = "Submitted"
            });
        }
        else
        {
            existing.FileName = Clean(fileName, existing.FileName);
            existing.Remarks = Clean(remarks, existing.Remarks);
            existing.SubmittedOn = DateTime.Now;
            existing.Status = "Submitted";
            existing.Marks = null;
            existing.Feedback = "";
        }

        AddActivity("Assignment submitted", $"{student.FullName} submitted {assignment.CourseCode}: {assignment.Title}", "Assignments", "bi-upload", "success");
        AddNotification("Submission Received", $"Your submission for {assignment.CourseCode} has been sent to faculty.", "Assignments", "success");
        RaiseChanged();
    }

    public void GradeSubmission(string submissionId, int marks, string feedback)
    {
        var submission = Submissions.FirstOrDefault(s => s.Id == submissionId);
        if (submission is null) return;
        var assignment = CourseAssignments.FirstOrDefault(a => a.Id == submission.AssignmentId);
        submission.Marks = Math.Clamp(marks, 0, assignment?.Marks ?? marks);
        submission.Feedback = Clean(feedback, "Checked");
        submission.Status = "Checked";
        AddActivity("Submission checked", $"{submission.StudentName} got {submission.Marks} in {submission.CourseCode}", "Faculty", "bi-patch-check", "success");
        AddNotification("Assignment Checked", $"{submission.CourseCode} submission checked. Marks: {submission.Marks}.", "Assignments", "success");
        RaiseChanged();
    }

    public void SubmitProfileCorrectionRequest(string studentEmail, string fieldName, string currentValue, string requestedValue, string reason)
    {
        var student = FindStudentByEmail(studentEmail);
        if (student is null || string.IsNullOrWhiteSpace(requestedValue)) return;
        ProfileCorrectionRequests.Insert(0, new ProfileCorrectionRequest
        {
            Id = NewId(),
            StudentEmail = student.Email,
            StudentId = student.StudentId,
            StudentName = student.FullName,
            FieldName = Clean(fieldName, "Profile Field"),
            CurrentValue = currentValue,
            RequestedValue = requestedValue.Trim(),
            Reason = Clean(reason, "Correction requested by student"),
            RequestedOn = DateTime.Now,
            Status = "Pending"
        });
        AddActivity("Profile correction requested", $"{student.FullName} requested {fieldName} change", "Registrar", "bi-pencil-square", "warning");
        AddNotification("Profile Request Sent", $"Your {fieldName} correction request is pending with Registrar Office.", "Profile", "warning");
        RaiseChanged();
    }

    public IEnumerable<ProfileCorrectionRequest> GetProfileRequestsForStudent(string email)
        => ProfileCorrectionRequests.Where(r => r.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.RequestedOn);

    public void ResolveProfileRequest(string id, bool approve, string registrarName)
    {
        var request = ProfileCorrectionRequests.FirstOrDefault(r => r.Id == id);
        if (request is null || request.Status != "Pending") return;
        request.Status = approve ? "Approved" : "Rejected";
        request.DecisionBy = Clean(registrarName, "Registrar Officer");
        request.DecisionOn = DateTime.Now;
        var student = FindStudentByEmail(request.StudentEmail);

        if (approve && student is not null)
        {
            var field = request.FieldName.ToLowerInvariant();
            if (field.Contains("phone")) student.Phone = request.RequestedValue;
            else if (field.Contains("address")) student.Address = request.RequestedValue;
            else if (field.Contains("guardian")) student.GuardianName = request.RequestedValue;
            else if (field.Contains("semester")) student.Semester = request.RequestedValue;
            else if (field.Contains("status")) student.RecordStatus = request.RequestedValue;
            else if (field.Contains("name")) student.FullName = request.RequestedValue;

            SyncMainProfile(student);
        }

        AddActivity(approve ? "Profile request approved" : "Profile request rejected", $"{request.StudentName} - {request.FieldName}", "Registrar", approve ? "bi-check-circle" : "bi-x-circle", approve ? "success" : "danger");
        AddNotification(approve ? "Profile Request Approved" : "Profile Request Rejected", $"Registrar {request.Status.ToLower()} your {request.FieldName} request.", "Profile", approve ? "success" : "danger");
        RaiseChanged();
    }

    public void RequestDocument(string studentEmail, string type, string purpose)
    {
        var student = FindStudentByEmail(studentEmail);
        if (student is null) return;
        DocumentRequests.Insert(0, new DocumentRequest
        {
            Id = NewId(),
            StudentEmail = student.Email,
            StudentId = student.StudentId,
            StudentName = student.FullName,
            Type = Clean(type, "Bonafide Certificate"),
            Purpose = Clean(purpose, "Academic requirement"),
            RequestedOn = DateTime.Now,
            Status = "Pending"
        });
        AddActivity("Document requested", $"{student.FullName} requested {type}", "Registrar", "bi-file-earmark-text", "primary");
        AddNotification("Document Request Sent", $"Your {type} request is pending with Registrar Office.", "Registrar", "info");
        RaiseChanged();
    }

    public IEnumerable<DocumentRequest> GetDocumentRequestsForStudent(string email)
        => DocumentRequests.Where(r => r.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.RequestedOn);

    public void ProcessDocumentRequest(string id, string status, string note)
    {
        var request = DocumentRequests.FirstOrDefault(r => r.Id == id);
        if (request is null) return;
        request.Status = Clean(status, "Ready");
        request.OfficeNote = Clean(note, "Processed by Registrar Office");
        request.ProcessedOn = DateTime.Now;
        AddActivity("Document request updated", $"{request.Type} for {request.StudentName}: {request.Status}", "Registrar", "bi-file-check", request.Status == "Ready" ? "success" : "warning");
        AddNotification("Document Request Updated", $"{request.Type}: {request.Status}.", "Registrar", request.Status == "Ready" ? "success" : "warning");
        RaiseChanged();
    }

    public void AddStudentRecord(string fullName, string studentId, string email, string department, string semester)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(email)) return;
        if (Students.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase) || s.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase))) return;
        Students.Insert(0, new PortalStudent
        {
            StudentId = studentId.Trim(),
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Department = Clean(department, "Computer Science"),
            Semester = Clean(semester, "I-A"),
            Cgpa = 0,
            Phone = "Not provided",
            Address = "Not provided",
            GuardianName = "Not provided",
            RecordStatus = "Active"
        });
        AddActivity("Student record created", $"Registrar created record for {fullName}", "Registrar", "bi-person-plus", "success");
        RaiseChanged();
    }

    public void UpdateStudentOfficialRecord(string email, string fullName, string department, string semester, double cgpa, string status)
    {
        var student = FindStudentByEmail(email);
        if (student is null) return;
        student.FullName = Clean(fullName, student.FullName);
        student.Department = Clean(department, student.Department);
        student.Semester = Clean(semester, student.Semester);
        student.Cgpa = Math.Clamp(cgpa, 0, 4);
        student.RecordStatus = Clean(status, student.RecordStatus);
        SyncMainProfile(student);
        AddActivity("Official record updated", $"Registrar updated {student.FullName}", "Registrar", "bi-database-check", "primary");
        AddNotification("Academic Record Updated", $"Registrar Office updated the record of {student.FullName}.", "Registrar", "info");
        RaiseChanged();
    }

    public void MarkAttendance(string courseId, DateTime date, string studentEmail, string status, string remarks, string markedBy)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        var student = FindStudentByEmail(studentEmail);
        if (course is null || student is null) return;
        var existing = AttendanceRecords.FirstOrDefault(r => r.CourseId == courseId && r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase) && r.Date.Date == date.Date);
        if (existing is null)
        {
            AttendanceRecords.Insert(0, new AttendanceRecord
            {
                Id = NewId(),
                CourseId = course.Id,
                CourseCode = course.Code,
                CourseTitle = course.Title,
                StudentEmail = student.Email,
                StudentId = student.StudentId,
                StudentName = student.FullName,
                Date = date.Date,
                Status = status,
                Remarks = Clean(remarks, "Marked from faculty portal"),
                MarkedBy = Clean(markedBy, course.FacultyName),
                MarkedAt = DateTime.Now
            });
        }
        else
        {
            existing.Status = status;
            existing.Remarks = Clean(remarks, existing.Remarks);
            existing.MarkedBy = Clean(markedBy, existing.MarkedBy);
            existing.MarkedAt = DateTime.Now;
        }

        AddActivity("Attendance marked", $"{student.FullName} - {course.Code}: {status}", "Attendance", "bi-clipboard-check", status == "Present" ? "success" : status == "Late" ? "warning" : "danger");
        RaiseChanged();
    }

    public IEnumerable<AttendanceRecord> GetAttendanceForStudent(string email)
        => AttendanceRecords.Where(r => r.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.Date);

    public IEnumerable<AttendanceRecord> GetAttendanceForCourse(string courseId)
        => AttendanceRecords.Where(r => r.CourseId == courseId).OrderByDescending(r => r.Date).ThenBy(r => r.StudentName);

    public int AttendancePercent(string email, string courseId)
    {
        var records = AttendanceRecords.Where(r => r.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase) && r.CourseId == courseId).ToList();
        if (records.Count == 0) return 100;
        var present = records.Count(r => r.Status == "Present" || r.Status == "Late" || r.Status == "Leave");
        return (int)Math.Round((present * 100.0) / records.Count);
    }

    public int OverallAttendancePercent(string email)
    {
        var records = AttendanceRecords.Where(r => r.StudentEmail.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();
        if (records.Count == 0) return 100;
        var present = records.Count(r => r.Status == "Present" || r.Status == "Late" || r.Status == "Leave");
        return (int)Math.Round((present * 100.0) / records.Count);
    }

    public void PublishClassroomPost(string courseId, string title, string message, string type, string postedBy, bool notify = true)
    {
        var course = Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return;
        ClassroomPosts.Insert(0, new ClassroomPost
        {
            Id = NewId(),
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseTitle = course.Title,
            Title = Clean(title, "Classroom Update"),
            Message = Clean(message, "Please check course details."),
            Type = Clean(type, "Post"),
            PostedBy = Clean(postedBy, course.FacultyName),
            PostedOn = DateTime.Now
        });
        if (notify)
        {
            AddNotification("Classroom Updated", $"{course.Code}: {title}", "Classroom", "info");
            AddActivity("Classroom post", $"{course.Code}: {title}", "Classroom", "bi-easel2", "primary");
            RaiseChanged();
        }
    }

    public IEnumerable<ClassroomPost> GetClassroomPostsForStudent(string email)
    {
        var courseIds = GetStudentCourses(email).Select(c => c.Id).ToHashSet();
        return ClassroomPosts.Where(p => courseIds.Contains(p.CourseId)).OrderByDescending(p => p.PostedOn);
    }

    public IEnumerable<ClassroomPost> GetClassroomPostsForFaculty(string email)
    {
        var courseIds = GetCoursesForFaculty(email).Select(c => c.Id).ToHashSet();
        return ClassroomPosts.Where(p => courseIds.Contains(p.CourseId)).OrderByDescending(p => p.PostedOn);
    }

    public void SaveProfile(string fullName, string phone, string address, string bio, string skills)
    {
        Profile.Phone = Clean(phone, Profile.Phone);
        Profile.Address = Clean(address, Profile.Address);
        Profile.Bio = Clean(bio, Profile.Bio);
        Profile.Skills = Clean(skills, Profile.Skills);
        AddActivity("Profile contact updated", "Student updated non-official contact information", "Profile", "bi-person-check", "success");
        AddNotification("Profile Updated", "Contact/bio details were updated. Official academic fields remain locked by Registrar.", "Profile", "success");
        RaiseChanged();
    }

    public void AddAssignment(string title, string course, DateTime dueDate, string priority, string type, string teacher)
    {
        Assignments.Insert(0, new CampusAssignment
        {
            Id = NewId(),
            Title = Clean(title, "New Personal Task"),
            Course = Clean(course, "Personal"),
            Teacher = Clean(teacher, "Self"),
            DueDate = dueDate,
            Priority = Clean(priority, "Medium"),
            Type = Clean(type, "Individual"),
            Progress = 0,
            IsCompleted = false
        });
        AddActivity("Personal task added", title, "Assignments", "bi-plus-circle", "primary");
        RaiseChanged();
    }

    public void ToggleAssignment(string id)
    {
        var item = Assignments.FirstOrDefault(a => a.Id == id);
        if (item is null) return;
        item.IsCompleted = !item.IsCompleted;
        item.Progress = item.IsCompleted ? 100 : Math.Min(item.Progress, 90);
        AddActivity(item.IsCompleted ? "Task completed" : "Task reopened", item.Title, "Assignments", "bi-check-circle", item.IsCompleted ? "success" : "warning");
        RaiseChanged();
    }

    public void UpdateAssignmentProgress(string id, int progress)
    {
        var item = Assignments.FirstOrDefault(a => a.Id == id);
        if (item is null) return;
        item.Progress = Math.Clamp(progress, 0, 100);
        item.IsCompleted = item.Progress == 100;
        RaiseChanged();
    }

    public void DeleteAssignment(string id)
    {
        var item = Assignments.FirstOrDefault(a => a.Id == id);
        if (item is null) return;
        Assignments.Remove(item);
        AddActivity("Personal task deleted", item.Title, "Assignments", "bi-trash", "danger");
        RaiseChanged();
    }

    public void AddSchedule(string course, string day, string startTime, string endTime, string room, string teacher, string type)
    {
        Schedule.Insert(0, new CampusSchedule
        {
            Id = NewId(), Course = Clean(course, "Study Block"), Day = Clean(day, "Monday"),
            StartTime = Clean(startTime, "09:00"), EndTime = Clean(endTime, "10:30"),
            Room = Clean(room, "Library"), Teacher = Clean(teacher, "Self"), Type = Clean(type, "Study")
        });
        AddActivity("Personal schedule added", course, "Schedule", "bi-calendar-plus", "primary");
        RaiseChanged();
    }

    public void UpdateSchedule(string id, string course, string day, string startTime, string endTime, string room, string teacher, string type)
    {
        var item = Schedule.FirstOrDefault(s => s.Id == id);
        if (item is null) return;
        item.Course = Clean(course, item.Course); item.Day = Clean(day, item.Day); item.StartTime = Clean(startTime, item.StartTime);
        item.EndTime = Clean(endTime, item.EndTime); item.Room = Clean(room, item.Room); item.Teacher = Clean(teacher, item.Teacher); item.Type = Clean(type, item.Type);
        AddActivity("Personal schedule updated", item.Course, "Schedule", "bi-calendar-check", "success");
        RaiseChanged();
    }

    public void DeleteSchedule(string id)
    {
        var item = Schedule.FirstOrDefault(s => s.Id == id);
        if (item is null) return;
        Schedule.Remove(item);
        AddActivity("Personal schedule removed", item.Course, "Schedule", "bi-trash", "danger");
        RaiseChanged();
    }

    public void ReportLostFound(string title, string category, string type, string location, string description)
    {
        LostFound.Insert(0, new LostFoundItem
        {
            Id = NewId(), Title = Clean(title, "Campus Item"), Category = Clean(category, "Other"), Type = Clean(type, "Lost"),
            Location = Clean(location, "Air University"), Description = Clean(description, "Reported from SmartCampus"),
            Status = "Open", ReportedBy = Profile.FullName, ReportedOn = DateTime.Now
        });
        AddActivity("Lost & Found report", title, "Lost & Found", "bi-search-heart", "warning");
        AddNotification("Lost & Found Updated", title, "Lost & Found", "info");
        RaiseChanged();
    }

    public void ClaimLostFound(string id)
    {
        var item = LostFound.FirstOrDefault(l => l.Id == id);
        if (item is null) return;
        item.Status = item.Status == "Open" ? "Claimed" : "Open";
        AddActivity("Lost & Found status", $"{item.Title}: {item.Status}", "Lost & Found", "bi-check2-circle", "success");
        RaiseChanged();
    }

    public void DeleteLostFound(string id)
    {
        var item = LostFound.FirstOrDefault(l => l.Id == id);
        if (item is null) return;
        LostFound.Remove(item);
        AddActivity("Lost & Found deleted", item.Title, "Lost & Found", "bi-trash", "danger");
        RaiseChanged();
    }

    public void AddEvent(string title, string category, DateTime date, string time, string venue, int capacity)
    {
        Events.Insert(0, new CampusEvent
        {
            Id = NewId(), Title = Clean(title, "Campus Event"), Category = Clean(category, "Academic"), Date = date,
            Time = Clean(time, "10:00 AM"), Venue = Clean(venue, "Main Auditorium"), Capacity = Math.Max(10, capacity),
            Registered = 0, Organizer = "Student Affairs", IsRegistered = false
        });
        AddActivity("Event posted", title, "Events", "bi-calendar-event", "primary");
        AddNotification("New Campus Event", title, "Events", "info");
        RaiseChanged();
    }

    public void RegisterEvent(string id)
    {
        var item = Events.FirstOrDefault(e => e.Id == id);
        if (item is null) return;
        if (item.IsRegistered) { item.Registered = Math.Max(0, item.Registered - 1); item.IsRegistered = false; }
        else if (item.SeatsLeft > 0) { item.Registered++; item.IsRegistered = true; }
        AddActivity(item.IsRegistered ? "Event joined" : "Event cancelled", item.Title, "Events", "bi-ticket-perforated", item.IsRegistered ? "success" : "warning");
        RaiseChanged();
    }

    public void DeleteEvent(string id)
    {
        var item = Events.FirstOrDefault(e => e.Id == id);
        if (item is null) return;
        Events.Remove(item);
        AddActivity("Event deleted", item.Title, "Events", "bi-trash", "danger");
        RaiseChanged();
    }

    public void AddMarketplaceItem(string title, string category, string condition, decimal price, string details)
    {
        Marketplace.Insert(0, new MarketplaceItem
        {
            Id = NewId(), Title = Clean(title, "Campus Item"), Category = Clean(category, "Books"), Condition = Clean(condition, "Good"),
            Price = Math.Max(0, price), Details = Clean(details, "Listed by student"), Seller = Profile.FullName, Status = "Available", Rating = 4.8, PostedOn = DateTime.Now
        });
        AddActivity("Marketplace listing", title, "Marketplace", "bi-shop", "success");
        RaiseChanged();
    }

    public void ToggleListingSold(string id)
    {
        var item = Marketplace.FirstOrDefault(m => m.Id == id);
        if (item is null) return;
        item.Status = item.Status == "Available" ? "Sold" : "Available";
        AddActivity("Marketplace status", $"{item.Title}: {item.Status}", "Marketplace", "bi-bag-check", "success");
        RaiseChanged();
    }

    public void DeleteMarketplaceItem(string id)
    {
        var item = Marketplace.FirstOrDefault(m => m.Id == id);
        if (item is null) return;
        Marketplace.Remove(item);
        AddActivity("Marketplace deleted", item.Title, "Marketplace", "bi-trash", "danger");
        RaiseChanged();
    }

    public void BookRide(string id)
    {
        var item = Rides.FirstOrDefault(r => r.Id == id);
        if (item is null || item.SeatsLeft <= 0) return;
        item.SeatsLeft--;
        AddActivity("Ride booked", $"{item.Pickup} to {item.Destination}", "Ride Share", "bi-car-front", "success");
        AddNotification("Ride Booked", $"Seat booked with {item.DriverName} at {item.Time}.", "Ride Share", "success");
        RaiseChanged();
    }

    public void OfferRide(string driverName, string pickup, string destination, string time, int seats, decimal fare)
    {
        Rides.Insert(0, new RideOffer
        {
            Id = NewId(), DriverName = Clean(driverName, Profile.FullName), Pickup = Clean(pickup, "F-10 Markaz"),
            Destination = Clean(destination, "Air University"), Time = Clean(time, "08:00 AM"), SeatsLeft = Math.Max(1, seats),
            Fare = Math.Max(0, fare), DistanceKm = 12.4, DurationMin = 22
        });
        AddActivity("Ride offered", $"{pickup} to {destination}", "Ride Share", "bi-car-front", "primary");
        RaiseChanged();
    }

    public void UploadResource(string title, string course, string type, string description)
    {
        Resources.Insert(0, new StudyResource
        {
            Id = NewId(), Title = Clean(title, "Course Resource"), Course = Clean(course, "General"), Type = Clean(type, "PDF"),
            Description = Clean(description, "Uploaded to student library"), UploadedBy = "Faculty", UploadedOn = DateTime.Now, Downloads = 0
        });
        var c = Courses.FirstOrDefault(x => x.Code.Equals(course, StringComparison.OrdinalIgnoreCase));
        if (c is not null) PublishClassroomPost(c.Id, title, description, "Resource", "Faculty", false);
        AddActivity("Resource uploaded", title, "Resources", "bi-folder-plus", "success");
        AddNotification("New Resource", $"{course}: {title}", "Resources", "info");
        RaiseChanged();
    }

    public void DownloadResource(string id)
    {
        var item = Resources.FirstOrDefault(r => r.Id == id);
        if (item is null) return;
        item.Downloads++;
        AddActivity("Resource downloaded", item.Title, "Resources", "bi-download", "primary");
        RaiseChanged();
    }

    public void DeleteResource(string id)
    {
        var item = Resources.FirstOrDefault(r => r.Id == id);
        if (item is null) return;
        Resources.Remove(item);
        AddActivity("Resource deleted", item.Title, "Resources", "bi-trash", "danger");
        RaiseChanged();
    }

    public void AddNotification(string title, string message, string module, string tone = "info")
    {
        Notifications.Insert(0, new CampusNotification
        {
            Id = NewId(), Title = Clean(title, "Notification"), Message = Clean(message, "Portal update"),
            Module = Clean(module, "Dashboard"), Tone = tone, IsRead = false, CreatedAt = DateTime.Now
        });
    }

    public void ToggleNotificationRead(string id)
    {
        var item = Notifications.FirstOrDefault(n => n.Id == id);
        if (item is null) return;
        item.IsRead = !item.IsRead;
        RaiseChanged();
    }

    public void MarkAllNotificationsRead()
    {
        foreach (var n in Notifications) n.IsRead = true;
        RaiseChanged();
    }

    public void DeleteNotification(string id)
    {
        var item = Notifications.FirstOrDefault(n => n.Id == id);
        if (item is null) return;
        Notifications.Remove(item);
        RaiseChanged();
    }

    public void PushManualNotification(string title, string message, string module)
    {
        AddNotification(title, message, module, "info");
        AddActivity("Manual alert pushed", title, module, "bi-bell", "primary");
        RaiseChanged();
    }

    private static string Clean(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NewId() => Guid.NewGuid().ToString("N")[..10];

    private void AddActivity(string title, string message, string module, string icon, string tone)
    {
        Activities.Insert(0, new CampusActivity
        {
            Id = NewId(), Title = Clean(title, "Activity"), Message = Clean(message, "Portal action"), Module = Clean(module, "Dashboard"),
            Icon = Clean(icon, "bi-circle"), Tone = Clean(tone, "info"), CreatedAt = DateTime.Now
        });
    }

    private void SyncMainProfile(PortalStudent student)
    {
        if (!student.Email.Equals(Profile.Email, StringComparison.OrdinalIgnoreCase)) return;
        Profile.FullName = student.FullName;
        Profile.StudentId = student.StudentId;
        Profile.Email = student.Email;
        Profile.Department = student.Department;
        Profile.Semester = student.Semester;
        Profile.Cgpa = student.Cgpa;
        Profile.Phone = student.Phone;
        Profile.Address = student.Address;
    }

    private void RaiseChanged()
    {
        SaveSnapshotToDatabase();
        OnChange?.Invoke();
    }

    private void LoadFromDatabaseOrSeed()
    {
        using var db = _dbFactory.CreateDbContext();

        if (db.PortalStudents.Any())
        {
            Students.AddRange(db.PortalStudents.AsNoTracking().ToList());
            FacultyMembers.AddRange(db.PortalFaculty.AsNoTracking().ToList());
            Courses.AddRange(db.PortalCourses.AsNoTracking().ToList());
            Enrollments.AddRange(db.CourseEnrollments.AsNoTracking().ToList());
            CourseAssignments.AddRange(db.PortalAssignments.AsNoTracking().ToList());
            Submissions.AddRange(db.PortalSubmissions.AsNoTracking().ToList());
            Announcements.AddRange(db.PortalAnnouncements.AsNoTracking().ToList());
            AttendanceRecords.AddRange(db.AttendanceRecords.AsNoTracking().ToList());
            ProfileCorrectionRequests.AddRange(db.ProfileCorrectionRequests.AsNoTracking().ToList());
            DocumentRequests.AddRange(db.DocumentRequests.AsNoTracking().ToList());
            ClassroomPosts.AddRange(db.ClassroomPosts.AsNoTracking().ToList());

            Assignments.AddRange(db.CampusAssignments.AsNoTracking().ToList());
            Schedule.AddRange(db.CampusSchedules.AsNoTracking().ToList());
            LostFound.AddRange(db.LostFoundItems.AsNoTracking().ToList());
            Events.AddRange(db.CampusEvents.AsNoTracking().ToList());
            Marketplace.AddRange(db.MarketplaceItems.AsNoTracking().ToList());
            Rides.AddRange(db.RideOffers.AsNoTracking().ToList());
            Resources.AddRange(db.StudyResources.AsNoTracking().ToList());
            Notifications.AddRange(db.CampusNotifications.AsNoTracking().ToList());
            Activities.AddRange(db.CampusActivities.AsNoTracking().ToList());

            return;
        }

        SeedAcademicPortal();
        SeedAssignments();
        SeedSchedule();
        SeedLostFound();
        SeedEvents();
        SeedMarketplace();
        SeedRides();
        SeedResources();
        SeedNotifications();
        SeedActivities();

        SaveSnapshotToDatabase();
    }

    private void SaveSnapshotToDatabase()
    {
        using var db = _dbFactory.CreateDbContext();

        db.PortalStudents.RemoveRange(db.PortalStudents);
        db.PortalFaculty.RemoveRange(db.PortalFaculty);
        db.PortalCourses.RemoveRange(db.PortalCourses);
        db.CourseEnrollments.RemoveRange(db.CourseEnrollments);
        db.PortalAssignments.RemoveRange(db.PortalAssignments);
        db.PortalSubmissions.RemoveRange(db.PortalSubmissions);
        db.PortalAnnouncements.RemoveRange(db.PortalAnnouncements);
        db.AttendanceRecords.RemoveRange(db.AttendanceRecords);
        db.ProfileCorrectionRequests.RemoveRange(db.ProfileCorrectionRequests);
        db.DocumentRequests.RemoveRange(db.DocumentRequests);
        db.ClassroomPosts.RemoveRange(db.ClassroomPosts);

        db.CampusAssignments.RemoveRange(db.CampusAssignments);
        db.CampusSchedules.RemoveRange(db.CampusSchedules);
        db.LostFoundItems.RemoveRange(db.LostFoundItems);
        db.CampusEvents.RemoveRange(db.CampusEvents);
        db.MarketplaceItems.RemoveRange(db.MarketplaceItems);
        db.RideOffers.RemoveRange(db.RideOffers);
        db.StudyResources.RemoveRange(db.StudyResources);
        db.CampusNotifications.RemoveRange(db.CampusNotifications);
        db.CampusActivities.RemoveRange(db.CampusActivities);

        db.SaveChanges();

        db.PortalStudents.AddRange(Students);
        db.PortalFaculty.AddRange(FacultyMembers);
        db.PortalCourses.AddRange(Courses);
        db.CourseEnrollments.AddRange(Enrollments);
        db.PortalAssignments.AddRange(CourseAssignments);
        db.PortalSubmissions.AddRange(Submissions);
        db.PortalAnnouncements.AddRange(Announcements);
        db.AttendanceRecords.AddRange(AttendanceRecords);
        db.ProfileCorrectionRequests.AddRange(ProfileCorrectionRequests);
        db.DocumentRequests.AddRange(DocumentRequests);
        db.ClassroomPosts.AddRange(ClassroomPosts);

        db.CampusAssignments.AddRange(Assignments);
        db.CampusSchedules.AddRange(Schedule);
        db.LostFoundItems.AddRange(LostFound);
        db.CampusEvents.AddRange(Events);
        db.MarketplaceItems.AddRange(Marketplace);
        db.RideOffers.AddRange(Rides);
        db.StudyResources.AddRange(Resources);
        db.CampusNotifications.AddRange(Notifications);
        db.CampusActivities.AddRange(Activities);

        db.SaveChanges();
    }

    private void SeedAcademicPortal()
    {
        Students.AddRange(new[]
        {
            new PortalStudent { StudentId = "241844", FullName = "Bilal Khalid", Email = "241844@students.au.edu.pk", Department = "Computer Science", Semester = "IV-C", Cgpa = 3.72, Phone = "+92 300 0000000", Address = "Air University, Islamabad", GuardianName = "Khalid Mehmood", RecordStatus = "Active" },
            new PortalStudent { StudentId = "241856", FullName = "Muhammad Zakria", Email = "s241856@students.au.edu.pk", Department = "Computer Science", Semester = "IV-C", Cgpa = 3.68, Phone = "+92 300 1111111", Address = "Islamabad", GuardianName = "Muhammad Aslam", RecordStatus = "Active" },
            new PortalStudent { StudentId = "241829", FullName = "Ahmad Shafique", Email = "s241829@students.au.edu.pk", Department = "Computer Science", Semester = "IV-C", Cgpa = 3.61, Phone = "+92 300 2222222", Address = "Rawalpindi", GuardianName = "Shafique Ahmed", RecordStatus = "Active" },
            new PortalStudent { StudentId = "241900", FullName = "Ayesha Tariq", Email = "s241900@students.au.edu.pk", Department = "Software Engineering", Semester = "II-A", Cgpa = 3.44, Phone = "+92 300 3333333", Address = "F-10 Islamabad", GuardianName = "Tariq Mehmood", RecordStatus = "Active" },
            new PortalStudent { StudentId = "241755", FullName = "Ali Hassan", Email = "s241755@students.au.edu.pk", Department = "Electrical Engineering", Semester = "VI-B", Cgpa = 3.12, Phone = "+92 300 4444444", Address = "G-11 Islamabad", GuardianName = "Hassan Raza", RecordStatus = "On Hold" }
        });

        FacultyMembers.AddRange(new[]
        {
            new PortalFaculty { FacultyId = "F-101", FullName = "Mr. Hafiz Obaid Ullah", Email = "hod.cs@au.edu.pk", Department = "Computer Science", Designation = "HOD / Lab Instructor" },
            new PortalFaculty { FacultyId = "F-102", FullName = "Dr. Bilal Raza", Email = "bilal.raza@au.edu.pk", Department = "Computer Science", Designation = "Assistant Professor" },
            new PortalFaculty { FacultyId = "F-103", FullName = "Ms. Sana Rehman", Email = "sana.rehman@au.edu.pk", Department = "Computer Science", Designation = "Lecturer" },
            new PortalFaculty { FacultyId = "F-104", FullName = "Dr. Arif Nawaz", Email = "arif.nawaz@au.edu.pk", Department = "Computer Science", Designation = "Associate Professor" }
        });

        Courses.AddRange(new[]
        {
            new PortalCourse { Id = "c-vp", Code = "CS-284L", Title = "Visual Programming Lab", Department = "Computer Science", Semester = "IV-C", CreditHours = 1, Capacity = 45, FacultyEmail = "bilal.raza@au.edu.pk", FacultyName = "Dr. Bilal Raza", Day = "Monday", StartTime = "09:00", EndTime = "11:00", Room = "CS Lab 2", IsOpenForEnrollment = true, ClassroomCode = "AU-CS284L-26", ClassroomLink = "https://classroom.google.com/c/NzA4NTYw" },
            new PortalCourse { Id = "c-dsa", Code = "CS-211", Title = "Data Structures", Department = "Computer Science", Semester = "IV-C", CreditHours = 3, Capacity = 50, FacultyEmail = "sana.rehman@au.edu.pk", FacultyName = "Ms. Sana Rehman", Day = "Tuesday", StartTime = "10:00", EndTime = "11:30", Room = "Block B-204", IsOpenForEnrollment = true, ClassroomCode = "AU-CS211-26", ClassroomLink = "https://classroom.google.com/c/NzA4NTYx" },
            new PortalCourse { Id = "c-os", Code = "CS-302", Title = "Operating Systems", Department = "Computer Science", Semester = "IV-C", CreditHours = 3, Capacity = 48, FacultyEmail = "arif.nawaz@au.edu.pk", FacultyName = "Dr. Arif Nawaz", Day = "Wednesday", StartTime = "01:00", EndTime = "02:30", Room = "Seminar Hall C", IsOpenForEnrollment = false, ClassroomCode = "AU-CS302-26", ClassroomLink = "https://classroom.google.com/c/NzA4NTYy" },
            new PortalCourse { Id = "c-cn", Code = "CS-315", Title = "Computer Networks", Department = "Computer Science", Semester = "IV-C", CreditHours = 3, Capacity = 45, FacultyEmail = "bilal.raza@au.edu.pk", FacultyName = "Dr. Bilal Raza", Day = "Thursday", StartTime = "11:00", EndTime = "12:30", Room = "Network Lab", IsOpenForEnrollment = true, ClassroomCode = "AU-CS315-26", ClassroomLink = "https://classroom.google.com/c/NzA4NTYz" }
        });

        Enrollments.AddRange(new[]
        {
            new CourseEnrollment { Id = NewId(), CourseId = "c-vp", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", EnrolledOn = DateTime.Now.AddDays(-20), Status = "Enrolled" },
            new CourseEnrollment { Id = NewId(), CourseId = "c-dsa", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", EnrolledOn = DateTime.Now.AddDays(-19), Status = "Enrolled" },
            new CourseEnrollment { Id = NewId(), CourseId = "c-vp", StudentEmail = "s241856@students.au.edu.pk", StudentId = "241856", StudentName = "Muhammad Zakria", EnrolledOn = DateTime.Now.AddDays(-18), Status = "Enrolled" },
            new CourseEnrollment { Id = NewId(), CourseId = "c-vp", StudentEmail = "s241829@students.au.edu.pk", StudentId = "241829", StudentName = "Ahmad Shafique", EnrolledOn = DateTime.Now.AddDays(-18), Status = "Enrolled" },
            new CourseEnrollment { Id = NewId(), CourseId = "c-cn", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", EnrolledOn = DateTime.Now.AddDays(-12), Status = "Enrolled" }
        });

        CourseAssignments.AddRange(new[]
        {
            new PortalAssignment { Id = "a-vp-proposal", CourseId = "c-vp", CourseCode = "CS-284L", CourseTitle = "Visual Programming Lab", Title = "Mini FYP Frontend Demo", Description = "Complete role-based frontend with live updates and demo flow.", DueDate = DateTime.Today.AddDays(1), Marks = 50, TeacherEmail = "bilal.raza@au.edu.pk", TeacherName = "Dr. Bilal Raza", CreatedAt = DateTime.Now.AddDays(-3) },
            new PortalAssignment { Id = "a-dsa-ll", CourseId = "c-dsa", CourseCode = "CS-211", CourseTitle = "Data Structures", Title = "Linked List Quiz Practice", Description = "Implement and dry run linked list operations.", DueDate = DateTime.Today.AddDays(4), Marks = 20, TeacherEmail = "sana.rehman@au.edu.pk", TeacherName = "Ms. Sana Rehman", CreatedAt = DateTime.Now.AddDays(-2) }
        });

        Announcements.AddRange(new[]
        {
            new PortalAnnouncement { Id = NewId(), Title = "Spring 2026 Add/Drop Window", Message = "Students can add/drop open courses before the deadline. HOD office controls availability and timetable.", Audience = "Students", CourseCode = "Students", PostedByName = "Registrar Office", PostedByRole = "Registrar", CreatedAt = DateTime.Now.AddHours(-9), Tone = "danger", IsPinned = true },
            new PortalAnnouncement { Id = NewId(), Title = "VP Lab Demo Reminder", Message = "Bring project report and be ready to explain role-based flow.", Audience = "Course", CourseId = "c-vp", CourseCode = "CS-284L", PostedByName = "Dr. Bilal Raza", PostedByRole = "Faculty", CreatedAt = DateTime.Now.AddHours(-3), Tone = "success", IsPinned = true }
        });

        AttendanceRecords.AddRange(new[]
        {
            new AttendanceRecord { Id = NewId(), CourseId = "c-vp", CourseCode = "CS-284L", CourseTitle = "Visual Programming Lab", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", Date = DateTime.Today.AddDays(-14), Status = "Present", Remarks = "On time", MarkedBy = "Dr. Bilal Raza", MarkedAt = DateTime.Now.AddDays(-14) },
            new AttendanceRecord { Id = NewId(), CourseId = "c-vp", CourseCode = "CS-284L", CourseTitle = "Visual Programming Lab", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", Date = DateTime.Today.AddDays(-7), Status = "Present", Remarks = "Lab work checked", MarkedBy = "Dr. Bilal Raza", MarkedAt = DateTime.Now.AddDays(-7) },
            new AttendanceRecord { Id = NewId(), CourseId = "c-dsa", CourseCode = "CS-211", CourseTitle = "Data Structures", StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", Date = DateTime.Today.AddDays(-6), Status = "Late", Remarks = "Arrived late", MarkedBy = "Ms. Sana Rehman", MarkedAt = DateTime.Now.AddDays(-6) },
            new AttendanceRecord { Id = NewId(), CourseId = "c-vp", CourseCode = "CS-284L", CourseTitle = "Visual Programming Lab", StudentEmail = "s241856@students.au.edu.pk", StudentId = "241856", StudentName = "Muhammad Zakria", Date = DateTime.Today.AddDays(-7), Status = "Absent", Remarks = "Not present", MarkedBy = "Dr. Bilal Raza", MarkedAt = DateTime.Now.AddDays(-7) }
        });

        ClassroomPosts.AddRange(new[]
        {
            new ClassroomPost { Id = NewId(), CourseId = "c-vp", CourseCode = "CS-284L", CourseTitle = "Visual Programming Lab", Title = "Project demo checklist", Message = "Prepare login flow, role-based modules, attendance, announcement, and submission demo.", Type = "Post", PostedBy = "Dr. Bilal Raza", PostedOn = DateTime.Now.AddHours(-5) },
            new ClassroomPost { Id = NewId(), CourseId = "c-dsa", CourseCode = "CS-211", CourseTitle = "Data Structures", Title = "Stack and Queue worksheet", Message = "Worksheet uploaded in resources. Attempt before next class.", Type = "Resource", PostedBy = "Ms. Sana Rehman", PostedOn = DateTime.Now.AddDays(-1) }
        });

        ProfileCorrectionRequests.Add(new ProfileCorrectionRequest { Id = NewId(), StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", FieldName = "Phone", CurrentValue = "+92 300 0000000", RequestedValue = "+92 300 5555555", Reason = "Number changed", RequestedOn = DateTime.Now.AddHours(-2), Status = "Pending" });
        DocumentRequests.Add(new DocumentRequest { Id = NewId(), StudentEmail = "241844@students.au.edu.pk", StudentId = "241844", StudentName = "Bilal Khalid", Type = "Bonafide Certificate", Purpose = "Internship application", RequestedOn = DateTime.Now.AddDays(-1), Status = "Pending" });
    }

    private void SeedAssignments()
    {
        Assignments.AddRange(new[]
        {
            new CampusAssignment { Id = NewId(), Title = "Prepare supervisor demo script", Course = "Personal", Teacher = "Self", DueDate = DateTime.Today.AddDays(1), Priority = "High", Type = "Personal", Progress = 65, IsCompleted = false },
            new CampusAssignment { Id = NewId(), Title = "Collect screenshots for report", Course = "Personal", Teacher = "Self", DueDate = DateTime.Today, Priority = "Medium", Type = "Personal", Progress = 80, IsCompleted = false }
        });
    }

    private void SeedSchedule()
    {
        Schedule.AddRange(new[]
        {
            new CampusSchedule { Id = NewId(), Course = "Mini FYP polishing", Day = "Friday", StartTime = "04:00", EndTime = "06:00", Room = "Home", Teacher = "Self", Type = "Study" },
            new CampusSchedule { Id = NewId(), Course = "Viva preparation", Day = "Saturday", StartTime = "09:00", EndTime = "10:00", Room = "Library", Teacher = "Group", Type = "Meeting" }
        });
    }

    private void SeedLostFound()
    {
        LostFound.AddRange(new[]
        {
            new LostFoundItem { Id = NewId(), Title = "Black USB Drive", Category = "Electronics", Type = "Lost", Location = "CS Lab 2", Description = "Contains project files", Status = "Open", ReportedBy = "Bilal Khalid", ReportedOn = DateTime.Now.AddHours(-4) },
            new LostFoundItem { Id = NewId(), Title = "Student ID Card", Category = "Card", Type = "Found", Location = "Cafeteria", Description = "Air University student card", Status = "Open", ReportedBy = "Security Desk", ReportedOn = DateTime.Now.AddDays(-1) }
        });
    }

    private void SeedEvents()
    {
        Events.AddRange(new[]
        {
            new CampusEvent { Id = NewId(), Title = "Blazor Web Dev Bootcamp", Category = "Workshop", Date = DateTime.Today.AddDays(3), Time = "10:00 AM", Venue = "Computer Lab B", Capacity = 80, Registered = 22, IsRegistered = false, Organizer = "CS Society" },
            new CampusEvent { Id = NewId(), Title = "National AI & Tech Olympiad 2026", Category = "Competition", Date = DateTime.Today.AddDays(10), Time = "09:00 AM", Venue = "AU Auditorium", Capacity = 300, Registered = 247, IsRegistered = true, Organizer = "Air University" },
            new CampusEvent { Id = NewId(), Title = "Inter-Department Cricket Cup", Category = "Sports", Date = DateTime.Today.AddDays(5), Time = "08:00 AM", Venue = "AU Sports Ground", Capacity = 100, Registered = 68, IsRegistered = false, Organizer = "Sports Office" }
        });
    }

    private void SeedMarketplace()
    {
        Marketplace.AddRange(new[]
        {
            new MarketplaceItem { Id = NewId(), Title = "Data Structures - Shaffer", Category = "Books", Condition = "Like New", Price = 650, Details = "3rd Edition, good condition", Seller = "Muhammad Zakria", Status = "Available", Rating = 4.9, PostedOn = DateTime.Now.AddDays(-2) },
            new MarketplaceItem { Id = NewId(), Title = "JBL Tune 510BT Headphones", Category = "Electronics", Condition = "Like New", Price = 3500, Details = "Wireless, blue", Seller = "Bilal Khalid", Status = "Available", Rating = 4.8, PostedOn = DateTime.Now.AddDays(-1) },
            new MarketplaceItem { Id = NewId(), Title = "Laptop Backpack", Category = "Accessories", Condition = "Good", Price = 2200, Details = "15.6 inch laptop bag", Seller = "Nida Aslam", Status = "Sold", Rating = 4.7, PostedOn = DateTime.Now.AddDays(-3) }
        });
    }

    private void SeedRides()
    {
        Rides.AddRange(new[]
        {
            new RideOffer { Id = NewId(), DriverName = "Ahmad Shafique", Pickup = "G-11", Destination = "Air University", Time = "07:30 AM", SeatsLeft = 2, Fare = 150, DistanceKm = 11.2, DurationMin = 25 },
            new RideOffer { Id = NewId(), DriverName = "Bilal Khalid", Pickup = "F-10", Destination = "Air University", Time = "08:00 AM", SeatsLeft = 1, Fare = 120, DistanceKm = 12.4, DurationMin = 22 },
            new RideOffer { Id = NewId(), DriverName = "Sara Khan", Pickup = "G-9", Destination = "Air University", Time = "08:15 AM", SeatsLeft = 3, Fare = 100, DistanceKm = 9.8, DurationMin = 18 }
        });
    }

    private void SeedResources()
    {
        Resources.AddRange(new[]
        {
            new StudyResource { Id = NewId(), Title = "VP Lab Blazor Routing Notes", Course = "CS-284L", Type = "PDF", Description = "Routes, layouts, auth guard and components", UploadedBy = "Dr. Bilal Raza", UploadedOn = DateTime.Now.AddDays(-2), Downloads = 34 },
            new StudyResource { Id = NewId(), Title = "Operating Systems Scheduling Slides", Course = "CS-302", Type = "PPT", Description = "FCFS, SJF, Round Robin", UploadedBy = "Dr. Arif Nawaz", UploadedOn = DateTime.Now.AddDays(-5), Downloads = 61 },
            new StudyResource { Id = NewId(), Title = "DSA Linked List Practice", Course = "CS-211", Type = "PDF", Description = "Practice questions with dry run", UploadedBy = "Ms. Sana", UploadedOn = DateTime.Now.AddDays(-1), Downloads = 19 }
        });
    }

    private void SeedNotifications()
    {
        AddNotification("Registrar Request Pending", "Your profile correction and bonafide certificate request are waiting for registrar action.", "Registrar", "warning");
        AddNotification("Attendance Updated", "Your VP Lab attendance is available in the attendance module.", "Attendance", "success");
        AddNotification("New Classroom Post", "VP Lab demo checklist was posted in Course Rooms.", "Classroom", "info");
        AddNotification("Ride Matched", "A ride from F-10 to Air University is available at 08:00 AM.", "Ride Share", "success");
    }

    private void SeedActivities()
    {
        AddActivity("Portal live", "Role-based university flow is active", "Dashboard", "bi-lightning-charge", "primary");
        AddActivity("Registrar workflow ready", "Student records, correction requests, documents and enrollments are connected", "Registrar", "bi-building-lock", "danger");
        AddActivity("Attendance system ready", "Faculty can mark attendance and students can view percentage", "Attendance", "bi-clipboard-check", "success");
        AddActivity("GCR course rooms synced", "Announcements, assignments and resources appear inside student course rooms", "Classroom", "bi-easel2", "primary");
    }
}

public sealed class PortalStudent
{
    public string StudentId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Semester { get; set; } = "";
    public double Cgpa { get; set; }
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string GuardianName { get; set; } = "";
    public string RecordStatus { get; set; } = "Active";
    public string Initials => string.Join("", FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
}

public sealed class PortalFaculty
{
    public string FacultyId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Initials => string.Join("", FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
}

public sealed class PortalCourse
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Department { get; set; } = "";
    public string Semester { get; set; } = "";
    public int CreditHours { get; set; }
    public int Capacity { get; set; }
    public string FacultyEmail { get; set; } = "";
    public string FacultyName { get; set; } = "";
    public string Day { get; set; } = "Monday";
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "10:30";
    public string Room { get; set; } = "TBA";
    public bool IsOpenForEnrollment { get; set; } = true;
    public string ClassroomCode { get; set; } = "";
    public string ClassroomLink { get; set; } = "https://classroom.google.com/";
}

public sealed class CourseEnrollment
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime EnrolledOn { get; set; }
    public string Status { get; set; } = "Enrolled";
}

public sealed class PortalAssignment
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueDate { get; set; }
    public int Marks { get; set; }
    public string TeacherEmail { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue => DueDate.Date < DateTime.Today;
}

public sealed class PortalSubmission
{
    public string Id { get; set; } = "";
    public string AssignmentId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime SubmittedOn { get; set; }
    public string FileName { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? Marks { get; set; }
    public string Feedback { get; set; } = "";
}

public sealed class PortalAnnouncement
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Audience { get; set; } = "All";
    public string? CourseId { get; set; }
    public string CourseCode { get; set; } = "";
    public string PostedByName { get; set; } = "";
    public string PostedByRole { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Tone { get; set; } = "info";
    public bool IsPinned { get; set; }
}

public sealed class AttendanceRecord
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Present";
    public string Remarks { get; set; } = "";
    public string MarkedBy { get; set; } = "";
    public DateTime MarkedAt { get; set; }
}

public sealed class ProfileCorrectionRequest
{
    public string Id { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public string RequestedValue { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime RequestedOn { get; set; }
    public string Status { get; set; } = "Pending";
    public string DecisionBy { get; set; } = "";
    public DateTime? DecisionOn { get; set; }
}

public sealed class DocumentRequest
{
    public string Id { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Type { get; set; } = "";
    public string Purpose { get; set; } = "";
    public DateTime RequestedOn { get; set; }
    public string Status { get; set; } = "Pending";
    public string OfficeNote { get; set; } = "";
    public DateTime? ProcessedOn { get; set; }
}

public sealed class ClassroomPost
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "Post";
    public string PostedBy { get; set; } = "";
    public DateTime PostedOn { get; set; }
}

public sealed class StudentProfileVm
{
    public string FullName { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Semester { get; set; } = "";
    public double Cgpa { get; set; }
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Skills { get; set; } = "";
    public string Initials => string.Join("", FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
}

public sealed class CampusAssignment
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Course { get; set; } = "";
    public string Teacher { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Type { get; set; } = "Individual";
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;
    public string Status => IsCompleted ? "Completed" : IsOverdue ? "Overdue" : "Pending";
}

public sealed class CampusSchedule
{
    public string Id { get; set; } = "";
    public string Course { get; set; } = "";
    public string Day { get; set; } = "Monday";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string Room { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Type { get; set; } = "Lecture";
}

public sealed class LostFoundItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Other";
    public string Type { get; set; } = "Lost";
    public string Location { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string ReportedBy { get; set; } = "";
    public DateTime ReportedOn { get; set; }
}

public sealed class CampusEvent
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Workshop";
    public DateTime Date { get; set; }
    public string Time { get; set; } = "";
    public string Venue { get; set; } = "";
    public int Capacity { get; set; }
    public int Registered { get; set; }
    public bool IsRegistered { get; set; }
    public string Organizer { get; set; } = "";
    public int SeatsLeft => Math.Max(0, Capacity - Registered);
}

public sealed class MarketplaceItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Books";
    public string Condition { get; set; } = "Good";
    public decimal Price { get; set; }
    public string Details { get; set; } = "";
    public string Seller { get; set; } = "";
    public string Status { get; set; } = "Available";
    public double Rating { get; set; }
    public DateTime PostedOn { get; set; }
}

public sealed class RideOffer
{
    public string Id { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string Pickup { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Time { get; set; } = "";
    public int SeatsLeft { get; set; }
    public decimal Fare { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMin { get; set; }
}

public sealed class StudyResource
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Course { get; set; } = "";
    public string Type { get; set; } = "PDF";
    public string Description { get; set; } = "";
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedOn { get; set; }
    public int Downloads { get; set; }
}

public sealed class CampusNotification
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Module { get; set; } = "";
    public string Tone { get; set; } = "info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CampusActivity
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Module { get; set; } = "";
    public string Icon { get; set; } = "bi-circle";
    public string Tone { get; set; } = "info";
    public DateTime CreatedAt { get; set; }
}
