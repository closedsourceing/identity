using System.ComponentModel.DataAnnotations;

namespace Identity.Server.Models;

public sealed class LoginViewModel
{
    [Required]
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
