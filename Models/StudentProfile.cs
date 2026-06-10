namespace SmartCampus.Models;

public class StudentProfile
{
    public string FullName { get; set; } = "Muhammad Zakria";
    public string StudentId { get; set; } = "241856";
    public string Email { get; set; } = "s241856@students.au.edu.pk";
    public string Department { get; set; } = "Computer Science";
    public string Semester { get; set; } = "IV-C";
    public double Cgpa { get; set; } = 3.50;
    public string University { get; set; } = "Air University";

    public string Initials => string.Concat(
        FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w[0])).ToUpper();

    public List<string> Tags { get; set; } = new()
        { "Visual Programming", "Active Member", "Spring 2026" };

    public ActivityStats Stats { get; set; } = new();

    public List<RecentActivity> RecentActivities { get; set; } = new()
    {
        new("Submitted VP Lab Assignment",   "2h ago",     "#3b82f6"),
        new("Joined AI Workshop Event",      "Yesterday",  "#2ecc71"),
        new("Listed DS Book in Marketplace", "2 days ago", "#f97316"),
        new("Uploaded OS Notes to Library",  "3 days ago", "#a855f7"),
    };

    public List<CourseProgress> Courses { get; set; } = new()
    {
        new("Visual Programming", 88, "#3b82f6"),
        new("Data Structures",    75, "#2ecc71"),
        new("Operating Systems",  62, "#f97316"),
        new("Calculus III",       55, "#ef4444"),
    };
}

public class ActivityStats
{
    public int AssignmentsDone { get; set; } = 12;
    public int EventsJoined { get; set; } = 4;
    public int ItemsShared { get; set; } = 3;
    public int RidesPosted { get; set; } = 1;
}

public record RecentActivity(string Title, string Time, string Color);
public record CourseProgress(string Name, int Percent, string Color);

// ── Schedule Models ──────────────────────────────────────────────────────
public class TimetableSlot
{
    public string Day { get; set; } = "";
    public string Time { get; set; } = "";
    public string Course { get; set; } = "";
    public string ShortCode { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Room { get; set; } = "";
    public string Type { get; set; } = "Lecture";
    public string Color { get; set; } = "#3b82f6";
}

// ── Assignment / GCR Models ──────────────────────────────────────────────
public enum AssignmentStatus { Pending, Submitted, Late, Graded }

public class CourseCard
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Color { get; set; } = "#3b82f6";
    public string BgColor { get; set; } = "#eff6ff";
    public int Credits { get; set; } = 3;
    public List<AssignmentItem> Assignments { get; set; } = new();
    public List<CourseMaterial> Materials { get; set; } = new();
}

public class AssignmentItem
{
    public string Title { get; set; } = "";
    public string DueDate { get; set; } = "";
    public int TotalMarks { get; set; } = 10;
    public int? ObtainedMarks { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public string Description { get; set; } = "";
}

public class CourseMaterial
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "PDF";
    public string Date { get; set; } = "";
    public string Icon { get; set; } = "bi-file-earmark-pdf";
    public string Color { get; set; } = "#ef4444";
}

// ── Marketplace Models ───────────────────────────────────────────────────
public enum ListingCategory { Books, Notes, Study, Event, Other }

public class MarketplaceListing
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PostedBy { get; set; } = "";
    public string Initials { get; set; } = "";
    public string Department { get; set; } = "";
    public string TimeAgo { get; set; } = "";
    public ListingCategory Category { get; set; }
    public string? Price { get; set; }
    public string AvatarColor { get; set; } = "#3b82f6";
    public bool IsOwn { get; set; }
    public string BadgeColor { get; set; } = "#eff6ff";
    public string BadgeText { get; set; } = "#1d4ed8";
}