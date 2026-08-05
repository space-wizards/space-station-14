using Content.Shared.Changeling.Systems;
using Content.Shared.DamageOverlay;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DamageOverlay;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDamageOverlaySystem))]
public sealed partial class DamageOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public MobState State = MobState.Alive;

    [DataField, AutoNetworkedField]
    public float DeadLevel = 0f;

    [DataField, AutoNetworkedField]
    public float CritLevel = 0f;

    [DataField, AutoNetworkedField]
    public float PainLevel = 0f;

    [DataField, AutoNetworkedField]
    public float OxygenLevel = 0f;
}
