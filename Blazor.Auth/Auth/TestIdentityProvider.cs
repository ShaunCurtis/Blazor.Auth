/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

using System.Security.Claims;

namespace Blazor.Auth;

public static class TestIdentityProvider
{
    public const string Provider = "Dumb Provider";

    public static ClaimsIdentity GetIdentity(string userName)
    {
        var identity = identities.FirstOrDefault(item => item.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
        if (identity == null)
            return new ClaimsIdentity();

        return new ClaimsIdentity(identity.Claims, Provider);
    }

    private static List<TestIdentity> identities = new List<TestIdentity>()
        {
            VisitorIdentity, 
            UserIdentity, 
            AdminIdentity, 
        };

    public static List<string> GetTestIdentities()
    {
        var list = new List<string> { "None" };
        list.AddRange(identities.Select(identity => identity.Name!).ToList());
        return list;
    }

    public static Dictionary<Guid, string> TestIdentitiesDictionary()
    {
        var list = new Dictionary<Guid, string>();
        identities.ForEach(identity => list.Add(identity.Id, identity.Name));
        return list;
    }

    public static TestIdentity UserIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-100000000001"),
            Name = "User-1",
            Role = "UserRole"
        };

    public static TestIdentity VisitorIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-200000000001"),
            Name = "Visitor-1",
            Role = "VisitorRole"
        };

    public static TestIdentity AdminIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-300000000001"),
            Name = "Admin-1",
            Role = "AdminRole"
        };
}

public record TestIdentity
{
    public string Name { get; set; } = string.Empty;

    public Guid Id { get; set; } = Guid.Empty;

    public string Role { get; set; } = string.Empty;

    public Claim[] Claims
        => new[]{
            new Claim(ClaimTypes.Sid, this.Id.ToString()),
            new Claim(ClaimTypes.Name, this.Name),
            new Claim(ClaimTypes.Role, this.Role)
    };

}

