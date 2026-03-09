namespace Identity.Server.Models;

public sealed class ConsentViewModel
{
    public string ApplicationName { get; set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; set; } = [];
}
