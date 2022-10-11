using Content.Shared.MobState;

namespace Content.Server.Explosion.Components;
[RegisterComponent]
public sealed class TriggerOnMobstateChangeComponent : Component
{
    [DataField("mobState", required: true)]
    public DamageState MobState = DamageState.Alive;

    [DataField("gibOnDeath")]
    public bool GibOnDeath = false;
}
