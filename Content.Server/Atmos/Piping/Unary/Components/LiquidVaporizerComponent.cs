using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Piping.Unary.Components;

/// <summary>
/// Used for an entity that converts units of reagent into moles of gas.
/// </summary>
[RegisterComponent]
[Access(typeof(LiquidVaporizerSystem))]
public sealed partial class LiquidVaporizerComponent : Component
{

    /// <summary>
    /// The ID for the pipe node.
    /// </summary>
    [DataField]
    public string OutletId = "pipe";

    /// <summary>
    /// The ID of the slot for the container.
    /// </summary>
    [DataField]
    public string ContainerSlotId = "loadedContainerSlot";

    /// <summary>
    /// Load while running the device
    /// </summary>
    [DataField]
    public int PowerLoad = 500;

    /// <summary>
    /// Smoke entity to spawn.
    /// </summary>
    [DataField]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Flag to see if it needs running
    /// </summary>
    [DataField]
    public bool NeedBoiling = false;

    /// <summary>
    /// How much pressure can be in the outlet pipe, before the component stops.
    /// </summary>
    [DataField]
    public float MaxPipeOutputPressure = Atmospherics.MaxOutputPressure*0.5f;

    /// <summary>
    /// When should boiling stop based on evaporated mass per cycle.
    /// </summary>
    [DataField]
    public FixedPoint2 DesiredEvaporationRate = 10;


    /// <summary>
    /// How much volume of non-atmospheric vapors, the device will hold, before releasing them as smoke.
    /// </summary>
    [DataField]
    public FixedPoint2 PressureVolumeLimit = 10;

    /// <summary>
    /// How long smoke will live per unit of reagent smoked up.
    /// </summary>
    [DataField]
    public float VolumeToLifeTimeFactor = 0.25f;

    /// <summary>
    /// The maximum temperature that this device can heat solutions up.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxTemperature = 773.15;

    /// <summary>
    /// For a vaporizer, how many U of reagents are given per each mole of gas.
    /// </summary>
    /// <remarks>
    /// Derived from a standard of 500u per canister:
    /// 400u / 1871.71051 moles per canister
    /// </remarks>
    [DataField]
    public float ReagentToMolesMultiplier = 4.68f;
}
