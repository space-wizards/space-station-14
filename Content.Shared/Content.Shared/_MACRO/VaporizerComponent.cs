using Content.Shared._MACRO.Decapoids;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Decapoids.Components;
/// <summary>
/// Entities with this component (given it has GasTankComponent) converts a specific reagent into gas.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class VaporizerComponent : Component
{
    /// <summary>
    /// Solution name.
    /// </summary>
    [DataField]
    public string LiquidTank = "waterTank";

    /// <summary>
    /// Expected Reagent to process into gas.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> ExpectedReagent = "Water";

    /// <summary>
    /// What gas is output.
    /// </summary>
    [DataField]
    public Gas OutputGas = Gas.WaterVapor;

    /// <summary>
    /// The maximum amount of pressure that can be output.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxPressure = Atmospherics.OneAtmosphere * 10;

    /// <summary>
    /// The volume of reagent is multiplied by this in order to determine the amount of moles to add.
    /// </summary>
    [DataField]
    public float ReagentToMoles = 0.07f;

    /// <summary>
    /// How many units of reagent to consume per second.
    /// </summary>
    [DataField]
    public FixedPoint2 ReagentPerSecond = 0.09;

    /// <summary>
    /// Amount of time between each process.
    /// </summary>
    [DataField]
    public TimeSpan ProcessDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// A percentage for how filled the liquid tank should be before it is considered "Low"
    /// </summary>
    [DataField]
    public float LowPercentage = 0.2f;

    /// <summary>
    /// The next time to process.
    /// </summary>
    [DataField(readOnly: true), ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan NextProcess = TimeSpan.Zero;

    /// <summary>
    /// What state the vaporizer is currently in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public VaporizerState State = VaporizerState.Empty;
}
