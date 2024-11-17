using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Bar;

[RegisterComponent, NetworkedComponent]
public sealed partial class PolymorphGlassComponent : Component
{
    [DataField("glasses", required: true)]
    public List<EntProtoId> Glasses;
}
