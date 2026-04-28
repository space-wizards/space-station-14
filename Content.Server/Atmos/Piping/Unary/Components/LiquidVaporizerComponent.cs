using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Chemistry.Components;

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
    /// For a vaporizer, how many U of reagents are given per each mole of gas.
    /// </summary>
    /// <remarks>
    /// Derived from a standard of 500u per canister:
    /// 400u / 1871.71051 moles per canister
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReagentToMolesMultiplier = 4.679276275f;
}
