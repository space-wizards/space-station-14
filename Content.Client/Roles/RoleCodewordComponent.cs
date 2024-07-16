namespace Content.Client.Roles;

/// <summary>
/// 
/// </summary>
[RegisterComponent]
public sealed partial class RoleCodewordComponent : Component
{
    [DataField]
    public Dictionary<string, CodewordsData> RoleCodewords = new();
}

public struct CodewordsData
{
    public Color Color;
    public List<string> Codewords;

    public CodewordsData(Color color, List<string> codewords)
    {
        Color = color;
        Codewords = codewords;
    }
}
