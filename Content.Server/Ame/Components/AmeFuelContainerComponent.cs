using Content.Server.Ame.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Ame.Components;

/// <summary>
/// An antimatter containment cell used to handle the fuel for the AME.
/// When emagged it will leak fuel and explode constantly.
/// Requires <c>ExplosiveComponent</c> for emagging to work.
/// TODO: network and put in shared
/// </summary>
[RegisterComponent]
public sealed partial class AmeFuelContainerComponent : Component
{
    /// <summary>
    /// The amount of fuel in the jar.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int FuelAmount = 1000;

    /// <summary>
    /// The maximum fuel capacity of the jar.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int FuelCapacity = 1000;

    /// <summary>
    /// How long to wait between leak explosions.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LeakDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When to next leak fuel and explode.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextLeak = TimeSpan.Zero;

    /// <summary>
    /// How much fuel is lost per leak explosion.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int LeakedFuel = 100;
}
