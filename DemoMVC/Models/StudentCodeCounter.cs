using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models;

public class StudentCodeCounter
{
    [Key]
    [StringLength(6)]
    public string Prefix { get; set; } = string.Empty;

    [Range(0, 9999)]
    public int LastNumber { get; set; }
}
