using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public EntityUid? Effect = null;
    
    [DataField]
    public EntProtoId EffectPrototype = "EffectNightVision";
}
