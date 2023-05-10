using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Zombies;

/// <summary>
/// Temporary because diseases suck.
/// </summary>
[RegisterComponent]
public sealed class PendingZombieComponent : Component
{
    [DataField("damage")] public DamageSpecifier Damage = new()
    {
        DamageDict = new ()
        {
            { "Blunt", 0.5 },
            { "Cellular", 0.2 },
            { "Toxin", 0.2 },
        }
    };

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    // Scales damage over time.
    [DataField("infectedSecs")]
    public int InfectedSecs;
}
