using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models;

public class Khoa
{
    [Key]
    [StringLength(10)]
    [Display(Name = "Mã khoa")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
    [StringLength(100)]
    [Display(Name = "Tên khoa")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
