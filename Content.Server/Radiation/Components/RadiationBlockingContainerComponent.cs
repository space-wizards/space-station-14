using Content.Server.Radiation.Systems;

namespace Content.Server.Radiation.Components;

/// <summary>
///     Prevents entities from emitting or receiving radiation when placed inside this container.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationSystem))]
public sealed partial class RadiationBlockingContainerComponent : Component
{
    /// <summary>
    ///     How many rads per second does the blocker absorb?
    /// </summary>
    [DataField("resistance")]
    public float RadResistance = 1f;
}
