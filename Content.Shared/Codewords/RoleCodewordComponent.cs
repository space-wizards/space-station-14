using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Codewords;

/// <summary>
/// Used to display and highlight codewords in chat messages on the client.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CodewordSystem), Other = AccessPermissions.Read)]
public sealed partial class RoleCodewordComponent : Component
{
    /// <summary>
    /// Contains the codewords tied to a role.
    /// Key string should be unique for the role.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CodewordsData RoleCodewords;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct CodewordsData
{
    /// <summary>
    /// The Color these codewords appear as
    /// </summary>
    [DataField]
    public Color Color;

    /// <summary>
    /// The Codewords!!!
    /// </summary>
    [DataField]
    public List<string> Codewords;

    public CodewordsData(Color color, List<string> codewords)
    {
        Color = color;
        Codewords = codewords;
    }
}
