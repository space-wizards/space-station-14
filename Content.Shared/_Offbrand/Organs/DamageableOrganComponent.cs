using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DamageableOrganSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class DamageableOrganComponent : Component
{
    /// <summary>
    /// The maximum amount of damage that this entity's organ can take
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxDamage;

    /// <summary>
    /// The current amount of damage that this entity's organ has
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Damage;
}

/// <summary>
/// Raised on an organ when its damage is changed
/// </summary>
[ByRefEvent]
public readonly record struct OrganDamageChangedEvent(Entity<DamageableOrganComponent> Organ);
