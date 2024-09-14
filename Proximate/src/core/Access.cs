namespace Proximate;

public enum AccessLevel
{
    Open = 0x00,
    Required = 0x01,
    Admin = 0xF0
}


public class AccessOpen : AccessAttribute
{
    public AccessOpen() : base(AccessLevel.Open) {}
}

public class AccessRequired : AccessAttribute
{
    public AccessRequired() : base(AccessLevel.Required) {}
}

public class AccessAttribute : Attribute
{
    public static AccessLevel ParseLevel(string? value)
    {
        value = value?.ToLower().Trim();
        foreach (var level in Enum.GetValues<AccessLevel>())
        {
            if (Enum.GetName(level)!.ToLower() == value)
                return level;
        }
        return AccessLevel.Open;
    }

    public readonly AccessLevel level;

    public AccessAttribute(AccessLevel level)
        : base()
    {
        this.level = level;
    }
}