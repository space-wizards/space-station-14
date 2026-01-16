namespace Content.Shared.Database;


public enum BanType : byte
{
    Server,
    Role,
}

[Serializable]
public record struct BanRoleDef(string RoleType, string RoleId)
{
    public override string ToString()
    {
        return $"{RoleType}:{RoleId}";
    }
}
