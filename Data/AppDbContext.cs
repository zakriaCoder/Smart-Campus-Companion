using Microsoft.EntityFrameworkCore;
using SmartCampus.Models;
using SmartCampus.Services;

namespace SmartCampus.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<PortalStudent> PortalStudents => Set<PortalStudent>();
    public DbSet<PortalFaculty> PortalFaculty => Set<PortalFaculty>();
    public DbSet<PortalCourse> PortalCourses => Set<PortalCourse>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<PortalAssignment> PortalAssignments => Set<PortalAssignment>();
    public DbSet<PortalSubmission> PortalSubmissions => Set<PortalSubmission>();
    public DbSet<PortalAnnouncement> PortalAnnouncements => Set<PortalAnnouncement>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<ProfileCorrectionRequest> ProfileCorrectionRequests => Set<ProfileCorrectionRequest>();
    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
    public DbSet<ClassroomPost> ClassroomPosts => Set<ClassroomPost>();

    public DbSet<CampusAssignment> CampusAssignments => Set<CampusAssignment>();
    public DbSet<CampusSchedule> CampusSchedules => Set<CampusSchedule>();
    public DbSet<LostFoundItem> LostFoundItems => Set<LostFoundItem>();
    public DbSet<CampusEvent> CampusEvents => Set<CampusEvent>();
    public DbSet<MarketplaceItem> MarketplaceItems => Set<MarketplaceItem>();
    public DbSet<RideOffer> RideOffers => Set<RideOffer>();
    public DbSet<StudyResource> StudyResources => Set<StudyResource>();
    public DbSet<CampusNotification> CampusNotifications => Set<CampusNotification>();
    public DbSet<CampusActivity> CampusActivities => Set<CampusActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().HasKey(x => x.StudentId);
        modelBuilder.Entity<AppUser>().Property(x => x.Role).HasConversion<string>();

        modelBuilder.Entity<PortalStudent>().HasKey(x => x.StudentId);
        modelBuilder.Entity<PortalFaculty>().HasKey(x => x.FacultyId);
        modelBuilder.Entity<PortalCourse>().HasKey(x => x.Id);
        modelBuilder.Entity<CourseEnrollment>().HasKey(x => x.Id);
        modelBuilder.Entity<PortalAssignment>().HasKey(x => x.Id);
        modelBuilder.Entity<PortalSubmission>().HasKey(x => x.Id);
        modelBuilder.Entity<PortalAnnouncement>().HasKey(x => x.Id);
        modelBuilder.Entity<AttendanceRecord>().HasKey(x => x.Id);
        modelBuilder.Entity<ProfileCorrectionRequest>().HasKey(x => x.Id);
        modelBuilder.Entity<DocumentRequest>().HasKey(x => x.Id);
        modelBuilder.Entity<ClassroomPost>().HasKey(x => x.Id);

        modelBuilder.Entity<CampusAssignment>().HasKey(x => x.Id);
        modelBuilder.Entity<CampusSchedule>().HasKey(x => x.Id);
        modelBuilder.Entity<LostFoundItem>().HasKey(x => x.Id);
        modelBuilder.Entity<CampusEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<MarketplaceItem>().HasKey(x => x.Id);
        modelBuilder.Entity<RideOffer>().HasKey(x => x.Id);
        modelBuilder.Entity<StudyResource>().HasKey(x => x.Id);
        modelBuilder.Entity<CampusNotification>().HasKey(x => x.Id);
        modelBuilder.Entity<CampusActivity>().HasKey(x => x.Id);

        modelBuilder.Entity<MarketplaceItem>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<RideOffer>().Property(x => x.Fare).HasPrecision(18, 2);
    }
}