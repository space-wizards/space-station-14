using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.SalvoHud;

/// <summary>
/// This is used for a whole buncha bullshit that ultimately gives status icons to entities that display their PhysicalComposition data
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowMaterialCompositionIconsComponent : Component
{
    /// <summary>
    /// the action that makes this go boop
    /// </summary>
    [DataField]
    public EntProtoId ActivateActionProtoID = "ActionActivateSalvoHud";

    /// <summary>
    /// I hate actions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActivateActionEnt;

    /// <summary>
    /// the position where we last pinged from, used to keep a consistent set of things revealed.
    /// </summary>
    [DataField]
    public Vector2? LastPingPos = null;

    /// <summary>
    /// how long the current scan has been active for
    /// </summary>
    /// <remarks>
    /// Using an accumulator because I prefer them over timespans. should probably be a timespan though.
    /// </remarks>
    [DataField]
    public float Accumulator = 0;

    /// <summary>
    /// the current state of the scan
    /// </summary>
    [DataField]
    public SalvohudScanState CurrState = SalvohudScanState.Idle;

    /// <summary>
    /// how long to stay in the "in" state
    /// </summary>
    [DataField]
    public float InPeriod = 1;

    /// <summary>
    /// how long to stay in the "active" state
    /// </summary>
    [DataField]
    public float ActivePeriod = 9;

    [DataField]
    public float OutPeriod = 3;

    /// <summary>
    /// max range for the scan
    /// </summary>
    [DataField]
    public float MaxRadius = 7.5f;

    /// <summary>
    /// the current minimum range. set during out.
    /// </summary>
    [DataField]
    public float CurrMinRadius = 0;

    /// <summary>
    /// the current max range. set during in.
    /// </summary>
    [DataField]
    public float CurrRadius = 0;
}

public enum SalvohudScanState
{
    Idle,
    In,
    Active,
    Out
}
