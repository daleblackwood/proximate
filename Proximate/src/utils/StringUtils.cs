using System.Text.RegularExpressions;
using Swan;

namespace Proximate;

public static class StringUtils
{

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        return Regex.IsMatch(email, @"^[\w\-\.]+@([\w-]+\.)+[\w-]{2,}$");
    }

    public static string SanitiseEmail(string? email)
    {
        if (IsValidEmail(email) == false)
        {
            throw new Exception("Invalid email.");
        }
        return email!.ToLower().Trim();
    }

}