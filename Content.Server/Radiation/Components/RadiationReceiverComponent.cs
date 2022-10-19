using Content.Server.Radiation.Systems;
using Content.Shared.Radiation.Components;

namespace Content.Server.Radiation.Components;

/// <summary>
///     Marks component that receive radiation from <see cref="RadiationSourceComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationSystem))]
public sealed class RadiationReceiverComponent : Component
{
    /// <summary>
    ///     Does this object can receive radiation rays?
    ///     If false will ignore any radiation sources.
    /// </summary>
    [DataField("canReceive")]
    public bool CanReceive = true;

    /// <summary>
    ///     Current radiation value in rads per second.
    ///     Periodically updated by radiation system.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;
}

