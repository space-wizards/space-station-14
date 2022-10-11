using Content.Shared.MobState;

namespace Content.Server.Explosion.Components;
[RegisterComponent]
public sealed class TriggerOnMobstateChangeComponent : Component
{
    /// <summary>
    /// What state should trigger this?
    /// </summary>
    [DataField("mobState", required: true)]
    public DamageState MobState = DamageState.Alive;

    /// <summary>
    /// Should the entity gib?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gibOnDeath")]
    public bool GibOnDeath = false;

    /// <summary>
    /// Should the gibbed entity delete their items?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deleteItemsOnGib")]
    public bool DeleteItemsOnGib = false;
}
