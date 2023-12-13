using Content.Server.Atmos.Piping.Unary.EntitySystems;

namespace Content.Server.Atmos.Piping.Unary.Components;

[RegisterComponent]
[Access(typeof(GasCondenserSystem))]
public sealed partial class GasCondenserComponent : Component
{
    [DataField]
    public string Inlet = "pipe";

    [DataField]
    public string SolutionId = "tank";

    /// <summary>
    /// For a condenser, how many U of reagents are given per each mole of gas.
    /// </summary>
    /// <remarks>
    /// Derived from a standard of 500u per canister:
    /// 500u / 1871.71051 moles per canister
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MolesToReagentMultiplier = 0.2671f;
}
