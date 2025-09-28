using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight;

[RegisterComponent, NetworkedComponent]
public sealed partial class WithAttachedBodyPartsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Parts = [];
}
