using Proximate;

public class UserService : Service
{
    public class User
    {
        public required string email;
        public required string hash;
        public AccessLevel access = AccessLevel.Required;
    }

    private Dictionary<string, User> ucache = new();

    public Task AddUser(string email, string pw, AccessLevel access = AccessLevel.Required)
    {
        email = StringUtils.SanitiseEmail(email);
        var hash = AuthUtils.ComputeHash(pw);
        ucache.Add(email, new User { email = email, hash = hash, access = access });
        return Task.FromResult(true);
    }

    public Task<bool> HasUser(string email)
    {
        return Task.FromResult(ucache.ContainsKey(StringUtils.SanitiseEmail(email)));
    }

    public Task<User?> GetUser(string? email)
    {
        email = StringUtils.SanitiseEmail(email);
        if (ucache.ContainsKey(email))
        {
            return Task.FromResult(ucache[email] ?? null);
        }
        return Task.FromResult<User?>(null);
    }
}