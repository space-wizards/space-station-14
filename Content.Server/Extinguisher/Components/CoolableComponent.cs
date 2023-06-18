using Content.Shared.Extinguisher.Components;
using Content.Server.Extinguisher.Systems;

namespace Content.Server.Extinguisher.Components;

/// <summary>
///     Allows users to cool beaker
/// </summary>
[RegisterComponent]
[Access(typeof(CoolableSystem))]
public sealed class CoolableComponent : SharedCoolableComponent
{
    /// <summary>
    ///     How much water does it take to cool entity.
    /// </summary>
    [DataField("water")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float WaterConsumption = 1f;

    /// <summary>
    ///     How long does it take to cool entity.
    /// </summary>
    [DataField("time")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CoolingTime = TimeSpan.FromSeconds(1f);

    /// <summary>
    ///     How much we can cool a entity each threshold
    /// </summary>
    [DataField("coolingThreshold")] public float CoolingThreshold = -60f;
}
