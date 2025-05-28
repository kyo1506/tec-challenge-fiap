using Microsoft.AspNetCore.Identity;

namespace TecChallenge.Application.Extensions;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool FirstAccess { get; set; }
    public bool IsDeleted { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public int Level { get; set; }
    public bool IsDeleted { get; set; }
}