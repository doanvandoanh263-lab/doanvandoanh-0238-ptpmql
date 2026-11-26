using Microsoft.AspNetCore.Identity;

namespace TestMVC2026.Models;

public sealed class RoleRow
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int UserCount { get; init; }
}

public sealed class RoleFormViewModel
{
    public string? Id { get; init; }
    public string Name { get; set; } = string.Empty;
}

public sealed class PermissionRow
{
    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool Granted { get; init; }
}

public sealed class UserRow
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Roles { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
}

public sealed class UserRolesViewModel
{
    public IdentityUser User { get; init; } = new();
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlySet<string> SelectedRoles { get; init; } = new HashSet<string>();
}

public sealed class PermissionUpdateModel
{
    public string RoleId { get; set; } = string.Empty;
    public List<string> SelectedActions { get; set; } = [];
}
