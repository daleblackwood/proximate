using System.Collections.Specialized;
using Proximate;

public class AuthService : Service
{

    public async Task<string> Login(string email, string password)
    {
        var user = await Services.Get<UserService>().GetUser(email);
        var token = AuthUtils.GenerateToken(email, password, user?.hash);
        if (token == null)
            throw new Exception("Invalid credentials.");        
        return token!;
    }

    public Task<string> Login(NameValueCollection headers)
    {
        var auth = AuthUtils.ExtractBasicAuth(headers);
        if (auth == null || auth.Length < 2)
            throw new Exception("Invalid credentials.");
        return Login(auth[0]!, auth[1]!);
    }

    public Task<bool> CanAccess(string? token, AccessLevel requiredLevel)
    {
        try {
            var userLevel = AuthUtils.ConfirmTokenAccess(token);
            return Task.FromResult(userLevel >= requiredLevel);
        }
        catch {
            return Task.FromResult(false);
        }
    }
    
}