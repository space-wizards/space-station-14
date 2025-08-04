using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.PowerCell;

/// <summary>
/// Indicates that the entity's ActivatableUI requires power or else it closes.
/// </summary>
/// <remarks>
/// With ActivatableUI it will activate and deactivate when the ui is opened and closed, drawing power inbetween.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PowerCellDrawComponent : Component
{
    #region Prediction

    /// <summary>
    /// Whether there is any charge available to draw.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canDraw"), AutoNetworkedField]
    public bool CanDraw;

    /// <summary>
    /// Whether there is sufficient charge to use.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canUse"), AutoNetworkedField]
    public bool CanUse;

    #endregion

    /// <summary>
    /// Whether drawing is enabled.
    /// Having no cell will still disable it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// How much the entity draws while the UI is open.
    /// Set to 0 if you just wish to check for power upon opening the UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("drawRate")]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used whenever the entity is "used".
    /// This is used to ensure the UI won't open again without a minimum use power.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("useRate")]
    public float UseRate;

    /// <summary>
    /// When the next automatic power draw will occur
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How long to wait between power drawing.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}
