using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models;

public class Student
{
    [Key]
    [StringLength(10)]
    [Display(Name = "Mã sinh viên")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Range(2000, 2099, ErrorMessage = "Năm nhập học phải từ 2000 đến 2099.")]
    [Display(Name = "Năm nhập học")]
    public int EntryYear { get; set; } = 2021;

    [Required]
    [RegularExpression(@"^\d{2}$", ErrorMessage = "Mã hệ đào tạo phải gồm 2 chữ số.")]
    [Display(Name = "Mã hệ đào tạo")]
    public string TrainingSystemCode { get; set; } = "21";

    [Required]
    [RegularExpression(@"^\d{2}$", ErrorMessage = "Mã ngành phải gồm 2 chữ số.")]
    [Display(Name = "Mã ngành")]
    public string MajorCode { get; set; } = "05";

    [StringLength(200)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    [StringLength(10)]
    [Display(Name = "Khoa")]
    public string KhoaCode { get; set; } = string.Empty;

    public Khoa? Khoa { get; set; }
}
