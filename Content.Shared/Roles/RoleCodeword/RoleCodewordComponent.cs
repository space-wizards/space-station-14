using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Roles.RoleCodeword;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RoleCodewordComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, CodewordsData> RoleCodewords = new();

    public override bool SessionSpecific => true;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct CodewordsData
{
    [DataField]
    public Color Color;

    [DataField]
    public List<string> Codewords;

    public CodewordsData(Color color, List<string> codewords)
    {
        Color = color;
        Codewords = codewords;
    }
}
