using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Khoa> Khoas => Set<Khoa>();
        public DbSet<StudentCodeCounter> StudentCodeCounters => Set<StudentCodeCounter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Khoa>(entity =>
            {
                entity.ToTable("Khoa");
                entity.HasKey(khoa => khoa.Code);
                entity.Property(khoa => khoa.Code).HasMaxLength(10);
                entity.Property(khoa => khoa.Name).HasMaxLength(100).IsRequired();

                entity.HasData(
                    new Khoa { Code = "CNTT", Name = "Khoa Công nghệ Thông tin" },
                    new Khoa { Code = "DC", Name = "Khoa Địa chất" },
                    new Khoa { Code = "MO", Name = "Khoa Kỹ thuật Mỏ" },
                    new Khoa { Code = "KT", Name = "Khoa Kinh tế" },
                    new Khoa { Code = "DCK", Name = "Khoa Cơ - Điện" },
                    new Khoa { Code = "DCKT", Name = "Khoa Dầu khí" },
                    new Khoa { Code = "XD", Name = "Khoa Xây dựng" },
                    new Khoa { Code = "MT", Name = "Khoa Môi trường" },
                    new Khoa { Code = "TDBD", Name = "Khoa Trắc địa - Bản đồ và Quản lý đất đai" },
                    new Khoa { Code = "KHCB", Name = "Khoa Khoa học cơ bản" },
                    new Khoa { Code = "LLCT", Name = "Khoa Lý luận chính trị" },
                    new Khoa { Code = "GDQP", Name = "Khoa Giáo dục quốc phòng" });
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("SinhVien");
                entity.HasKey(student => student.StudentCode);
                entity.Property(student => student.StudentCode).HasMaxLength(10);
                entity.Property(student => student.FullName).HasMaxLength(100).IsRequired();
                entity.Property(student => student.Address).HasMaxLength(200);
                entity.Property(student => student.TrainingSystemCode).HasMaxLength(2).IsRequired();
                entity.Property(student => student.MajorCode).HasMaxLength(2).IsRequired();

                entity.HasOne(student => student.Khoa)
                    .WithMany(khoa => khoa.Students)
                    .HasForeignKey(student => student.KhoaCode)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentCodeCounter>(entity =>
            {
                entity.ToTable("StudentCodeCounter");
                entity.HasKey(counter => counter.Prefix);
                entity.Property(counter => counter.Prefix).HasMaxLength(6);
                entity.HasData(new StudentCodeCounter { Prefix = "212105", LastNumber = 238 });
            });
        }
    }
}