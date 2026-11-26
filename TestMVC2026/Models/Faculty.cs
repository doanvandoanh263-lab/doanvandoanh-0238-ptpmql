using System.ComponentModel.DataAnnotations;

namespace TestMVC2026.Models;

public sealed class Faculty
{
    [Key, Required, StringLength(10)]
    [Display(Name = "Mã khoa")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Tên khoa")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
