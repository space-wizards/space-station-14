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
        DamageDict = new Dictionary<string, FixedPoint2>()
        {
            { "Blunt", FixedPoint2.New(1) }
        }
    };

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;
}
