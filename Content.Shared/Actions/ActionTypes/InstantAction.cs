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
    [DataField("event")]
    [NonSerialized]
    public InstantActionEvent? Event;

    public InstantAction() { }
    public InstantAction(InstantAction toClone)
    {
        CopyFrom(toClone);
    }

    public override void CopyFrom(object objectToClone)
    {
        base.CopyFrom(objectToClone);

        // Server doesn't serialize events to us.
        // As such we don't want them to bulldoze any events we may have gotten locally.
        if (objectToClone is not InstantAction toClone)
            return;

        // Events should be re-usable, and shouldn't be modified during prediction.
        if (toClone.Event != null)
            Event = toClone.Event;
    }

    public override object Clone()
    {
        return new InstantAction(this);
    }
}
