using Content.Shared.FixedPoint;
using Content.Shared.Revenant.Systems;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(SharedRevenantSystem))]
public sealed partial class RevenantActionComponent : Component
{
    /// <summary>
    ///     The <see cref="TimeSpan"/> for which the entity is stunned.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     The <see cref="TimeSpan"/> for which the entity is made solid.
    /// </summary>
    [DataField]
    public TimeSpan CorporealTime = TimeSpan.FromSeconds(8);

    /// <summary>
    ///     The Essence cost for this ability.
    /// </summary>
    [DataField]
    public FixedPoint2 Cost;
}
