using Content.Server.StationEvents.Events;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GasLeakRule))]
public sealed partial class GasLeakRuleComponent : Component
{
    /// <summary>
    /// Gas types that can be selected for the leak event.
    /// </summary>
    [DataField]
    public Gas[] LeakableGases =
    {
        Gas.Ammonia,
        Gas.Plasma,
        Gas.Tritium,
        Gas.Frezon,
        Gas.WaterVapor,
    };

    /// <summary>
    /// Time remaining until the next gas addition to the leak tile.
    /// </summary>
    [DataField]
    public float TimeUntilLeak;

    /// <summary>
    /// Fixed interval in seconds between gas additions to the leak tile.
    /// </summary>
    [DataField]
    public float LeakCooldown = 1.0f;

    /// <summary>
    /// The station where the leak is located.
    /// </summary>
    [DataField]
    public EntityUid TargetStation;

    /// <summary>
    /// The specific grid where the leak is located.
    /// </summary>
    [DataField]
    public EntityUid TargetGrid;

    /// <summary>
    /// The tile coordinates where the leak is located.
    /// </summary>
    [DataField]
    public Vector2i TargetTile;

    /// <summary>
    /// The world coordinates of the leak location.
    /// </summary>
    [DataField]
    public EntityCoordinates TargetCoords;

    /// <summary>
    /// Whether a suitable tile for leaking has been found.
    /// </summary>
    [DataField]
    public bool FoundTile;

    /// <summary>
    /// The specific gas type currently leaking.
    /// </summary>
    [DataField]
    public Gas LeakGas;

    /// <summary>
    /// Current leak rate in moles per second.
    /// </summary>
    [DataField]
    public float MolesPerSecond;

    /// <summary>
    /// Minimum leak rate in moles per second.
    /// </summary>
    [DataField]
    public int MinimumMolesPerSecond = 80;

    /// <summary>
    /// Maximum leak rate in moles per second. Limited to give people time to flee.
    /// </summary>
    [DataField]
    public int MaximumMolesPerSecond = 200;

    /// <summary>
    /// Minimum total amount of gas to leak over the entire event duration.
    /// </summary>
    [DataField]
    public int MinimumGas = 1000;

    /// <summary>
    /// Maximum total amount of gas to leak over the entire event duration.
    /// </summary>
    [DataField]
    public int MaximumGas = 4000;

    /// <summary>
    /// Chance to create an ignition spark when the event ends.
    /// </summary>
    [DataField]
    public float SparkChance = 0.05f;
}
