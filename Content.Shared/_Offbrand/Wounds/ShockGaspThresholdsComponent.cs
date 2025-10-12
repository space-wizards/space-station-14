using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShockGaspThresholdsComponent : Component
{
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<EmotePrototype>> MessageThresholds = new();

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? CurrentMessage;
}
