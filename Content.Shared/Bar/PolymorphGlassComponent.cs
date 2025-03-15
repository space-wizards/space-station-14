using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Bar;

[RegisterComponent, NetworkedComponent]
public sealed partial class PolymorphGlassComponent : Component
{
    [DataField("glasses", required: true)]
    public Dictionary<EntProtoId, SpriteSpecifier> Glasses;
}
