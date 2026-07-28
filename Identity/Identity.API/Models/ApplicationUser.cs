namespace Identity.API.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
