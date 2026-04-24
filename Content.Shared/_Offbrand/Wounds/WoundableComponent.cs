using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// The maximum damages that can be acquired, and the factor that downcurves additional damage
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, (FixedPoint2 Base, FixedPoint2 Factor)> MaximumDamage = default!;

    /// <summary>
    ///     This <see cref="DamageContainerPrototype"/> specifies what damage types are supported by this component.
    ///     If null, all damage types will be supported.
    /// </summary>
    [DataField, AutoNetworkedField]
    // ReSharper disable once InconsistentNaming - This is wrong but fixing it is potentially annoying for downstreams.
    public ProtoId<DamageContainerPrototype>? DamageContainer;
}

/// <summary>
/// Raised on an entity to determine how much of its damage comes from wounds
/// </summary>
[ByRefEvent]
public record struct WoundGetDamageEvent(DamageSpecifier Accumulator);

/// <summary>
/// Raised when the values for a damage overlay may have changed
/// </summary>
[ByRefEvent]
public record struct PotentiallyUpdateDamageOverlayEvent(EntityUid Target);
