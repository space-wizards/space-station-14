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
            { "Blunt", 0.8 },
            { "Toxin", 0.2 },
        }
    };

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    // Scales damage over time.
    [DataField("infectedSecs")]
    public int InfectedSecs;

    // Infection warnings are shown as popups, times are in seconds.
    //   -ve times shown to initial zombies (once timer counts from -ve to 0 the infection starts)
    //   +ve warnings are in seconds after being bitten
    [DataField("infectionWarnings")]
    public Dictionary<int, string> InfectionWarnings = new()
    {
        {-45, "zombie-infection-warning"},
        {-30, "zombie-infection-warning"},
        {10, "zombie-infection-underway"},
        {25, "zombie-infection-underway"},
    };

}
