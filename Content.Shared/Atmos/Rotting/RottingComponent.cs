using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Atmos.Rotting;

/// <summary>
/// Tracking component for stuff that has started to rot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RottingComponent : Component
{
    /// <summary>
    /// Whether or not the rotting should deal damage
    /// </summary>
    [DataField("dealDamage"), ViewVariables(VVAccess.ReadWrite)]
    public bool DealDamage = true;

    /// <summary>
    /// When the next check will happen for rot progression + effects like damage and ammonia
    /// </summary>
    [DataField("nextRotUpdate", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextRotUpdate = TimeSpan.Zero;

    /// <summary>
    /// How long in between each rot update.
    /// </summary>
    [DataField("rotUpdateRate"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RotUpdateRate = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long has this thing been rotting?
    /// </summary>
    [DataField("totalRotTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TotalRotTime = TimeSpan.Zero;

    /// <summary>
    /// The damage dealt by rotting.
    /// </summary>
    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.06 },
            { "Cellular", 0.06 }
        }
    };
}
