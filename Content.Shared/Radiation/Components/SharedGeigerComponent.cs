using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Geiger counter that shows current radiation level.
///     Can be added as a component to clothes.
/// </summary>
[NetworkedComponent]
[Access(typeof(SharedGeigerSystem))]
public abstract class SharedGeigerComponent : Component
{
    /// <summary>
    ///     Is geiger counter currently active?
    ///     If false attached entity will ignore any radiation rays.
    /// </summary>
    [DataField("isEnabled")]
    public bool IsEnabled;

    /// <summary>
    ///     Should it shows examine message with current radiation level?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("showExamine")]
    public bool ShowExamine;

    /// <summary>
    ///     Current radiation level in rad per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;

    /// <summary>
    ///     Estimated radiation danger level.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public GeigerDangerLevel DangerLevel = GeigerDangerLevel.None;
}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
    public GeigerDangerLevel DangerLevel;
    public bool IsEnabled;
}

[Serializable, NetSerializable]
public enum GeigerDangerLevel : byte
{
    None,
    Low,
    Med,
    High,
    Extreme
}

[Serializable, NetSerializable]
public enum GeigerLayers : byte
{
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel,
    IsEnabled
}
