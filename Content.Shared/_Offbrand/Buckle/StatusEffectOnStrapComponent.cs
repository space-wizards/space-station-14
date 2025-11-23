using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Buckle;

/// <summary>
/// Applies a status effect to the strapped entity whilst it is strapped
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(StatusEffectOnStrapSystem))]
public sealed partial class StatusEffectOnStrapComponent : Component
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;
}
