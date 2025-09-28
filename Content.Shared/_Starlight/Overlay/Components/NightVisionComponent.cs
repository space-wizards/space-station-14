using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField]
    public EntProtoId EffectPrototype = "EffectNightVision";
}
