using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Waypointer.Components;

/// <summary>
/// This signifies an entity with an active waypointer trying to track something.
/// Gets given to an entity a player inhabits. This is NOT a pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveWaypointerComponent : Component
{
    /// <summary>
    /// The prototype of the waypointer visible for the owner of this component.
    /// The bool value determines whether the corresponding waypointer is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<WaypointerPrototype>, bool>? WaypointerProtoIds;

    /// <summary>
    /// Whether the waypointer system is enabled or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    #region Server-Updates
    /// <summary>
    /// The tick where the server sends an update of the newest location of tracked entities.
    /// This does not affect entities within PVS range or grids.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The delay between each update.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);
    #endregion

    #region Action
    /// <summary>
    /// The actual UID for the action entity. It'll be saved here when the component is initialized.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The prototype ID for the action.
    /// </summary>
    [DataField]
    public EntProtoId ActionProtoId = "ActionManageWaypointers";
    #endregion

    /// <summary>
    /// The resource path for the "Disable/Enable all waypointers" menu option.
    /// </summary>
    [DataField]
    public ResPath RadialMenuIconPath = new("Markers/Waypointers/waypointer_action.rsi");
}
