using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class WoundComponent : Component
{
    [DataField("healthDamage")] public int HealthDamage;

    [DataField("integrityDamage")] public int IntegrityDamage;
}
