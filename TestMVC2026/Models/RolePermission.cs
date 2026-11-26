using System.ComponentModel.DataAnnotations;

namespace TestMVC2026.Models;

public sealed class RolePermission
{
    [Required]
    public string RoleId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ControllerName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ActionName { get; set; } = string.Empty;
}
