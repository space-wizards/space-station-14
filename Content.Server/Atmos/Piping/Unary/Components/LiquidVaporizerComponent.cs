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
    /// The ID for the solution.
    /// </summary>
    [DataField]
    public string ContainerSlotId = "containerSlot";

    /// <summary>
    /// Load while running
    /// </summary>
    [DataField]
    public int PowerLoad = 500;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Load while running
    /// </summary>
    [DataField]
    public bool NeedBoiling = false;

    /// <summary>
    /// how strong is the output. not perfect and might slighty overpressure if a large solution is cooked off.
    /// </summary>
    [DataField]
    public float MaxPipeOutputPressure = Atmospherics.MaxOutputPressure*0.5f;

    /// <summary>
    /// when should boiling stop based on evaporated mass.
    /// </summary>
    [DataField]
    public FixedPoint2 DesiredEvaporationRate = 10;


    /// <summary>
    /// how much volume will be held back before the internal solution is turned into smoke (in a single step)
    /// </summary>
    [DataField]
    public FixedPoint2 PressureVolumeLimit = 10;

    /// <summary>
    /// how long smoke will live per unit of reagent smoked up.
    /// </summary>
    [DataField]
    public float VolumeToLifeTimeFactor = 0.25f;

    /// <summary>
    /// For a vaporizer, how many U of reagents are given per each mole of gas.
    /// </summary>
    /// <remarks>
    /// Derived from a standard of 500u per canister:
    /// 400u / 1871.71051 moles per canister
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReagentToMolesMultiplier = 4.68f;
}
