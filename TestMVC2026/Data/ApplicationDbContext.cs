using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Models;

namespace TestMVC2026.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Student>().HasKey(student => student.StudentCode);
        builder.Entity<Student>().HasOne(student => student.Faculty)
            .WithMany(faculty => faculty.Students)
            .HasForeignKey(student => student.FacultyCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Faculty>().HasKey(faculty => faculty.Code);
        builder.Entity<Faculty>().HasData(
            new Faculty { Code = "CNTT", Name = "Công nghệ thông tin" },
            new Faculty { Code = "KT", Name = "Kinh tế" },
            new Faculty { Code = "XD", Name = "Xây dựng" });
        builder.Entity<RolePermission>().HasKey(permission =>
            new { permission.RoleId, permission.ControllerName, permission.ActionName });
        builder.Entity<RolePermission>().HasOne<IdentityRole>()
            .WithMany()
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
