using Content.Shared.Atmos;
using Nett;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class GasLeakRuleComponent : Component
{
    public readonly Gas[] LeakableGases =
    {
        Gas.Miasma,
        Gas.Plasma,
        Gas.Tritium,
        Gas.Frezon,
    };

    /// <summary>
    ///     Running cooldown of how much time until another leak.
    /// </summary>
    public float _timeUntilLeak;

    /// <summary>
    ///     How long between more gas being added to the tile.
    /// </summary>
    public float LeakCooldown = 1.0f;

    // Event variables
    public EntityUid _targetStation;
    public EntityUid _targetGrid;
    public Vector2i _targetTile;
    public EntityCoordinates _targetCoords;
    public bool _foundTile;
    public Gas _leakGas;
    public float _molesPerSecond;
    public int MinimumMolesPerSecond = 20;
    public TimeSpan _endAfter = TimeSpan.MaxValue;

    /// <summary>
    ///     Don't want to make it too fast to give people time to flee.
    /// </summary>
    public int MaximumMolesPerSecond = 50;

    public int MinimumGas = 250;
    public int MaximumGas = 1000;
    public float SparkChance = 0.05f;
}
