using System.ComponentModel.DataAnnotations;

namespace TestMVC2026.Models;

public sealed class Student
{
    [Key, Required, StringLength(20)]
    [Display(Name = "Mã sinh viên")]
    public string StudentCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Range(1900, 2100)]
    [Display(Name = "Năm sinh")]
    public int BirthYear { get; set; } = 2000;

    [EmailAddress, StringLength(200)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(30)]
    [Display(Name = "Điện thoại")]
    public string? Phone { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Mã khoa")]
    public string FacultyCode { get; set; } = string.Empty;

    public Faculty? Faculty { get; set; }
}
