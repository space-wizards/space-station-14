using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

[RegisterComponent, NetworkedComponent]
public sealed partial class AntagDataComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<AntagLoadoutPrototype>, AntagData> Antagonists = new();
}
