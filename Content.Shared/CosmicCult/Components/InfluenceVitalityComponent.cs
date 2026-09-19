using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class InfluenceVitalityComponent : Component
{
    /// <summary>
    /// the timer used for ticking healing from vacuous vitality
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CheckTimer;

    /// <summary>
    /// the amount of time between the above timer's ticks
    /// </summary>
    [DataField]
    public TimeSpan CheckWait = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Passive healing factor for cultists with this influence.
    /// </summary>
    [DataField]
    public DamageSpecifier Healing = new()
    {
        DamageDict = new()
        {
            { "Blunt", 2},
            { "Slash", 2 },
            { "Piercing", 2 },
            { "Heat", 2},
            { "Shock", 2},
            { "Cold", 2},
            { "Poison", 2},
            { "Radiation", 2},
            { "Asphyxiation", 2 }
        }
    };
}
