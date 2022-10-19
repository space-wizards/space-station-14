using Robust.Shared.Serialization;

namespace Content.Shared.Actions.ActionTypes;

/// <summary>
///     Instantaneous action with no extra targeting information. Will result in <see cref="InstantActionEvent"/> being raised.
/// </summary>
[Serializable, NetSerializable]
[Virtual]
public class InstantAction : ActionType
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event", true)]
    public InstantActionEvent? Event { set; get; }

    [DataField("serverEvent", serverOnly: true)]
    public InstantActionEvent? ServerEvent { get; }
}
