using Content.Shared.Body.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

/// <summary>
/// Causes the bloodstream to drip blood droplets when damaged.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BloodstreamSystem))]
public sealed partial class BloodstreamDripOnDamageComponent : Component
{
    /// <summary>
    /// Minimal damage required.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Threshold = 5f;

    /// <summary>
    /// The chance of spawning droplets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Probability = 0.25f;

    /// <summary>
    /// The range for droplets to fly. From min (inclusive) to max (exclusive).
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax Range = new(2f, 4f);

    /// <summary>
    /// The force with which droplets will fly. From min (inclusive) to max (exclusive).
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax Force = new(2f, 3f);

    /// <summary>
    /// The number of droplets that will be spawned. From min (inclusive) to max (exclusive).
    /// </summary>
    [DataField, AutoNetworkedField]
    public (int Min, int Max) Amount = (1, 4); // TODO: Make MinMax generic for more types.

    /// <summary>
    /// The damage types that are allowed.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<DamageTypePrototype>> Allowed = [];
}
