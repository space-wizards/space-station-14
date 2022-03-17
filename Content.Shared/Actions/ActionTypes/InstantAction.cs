using Robust.Shared.Serialization;

namespace Content.Shared.Actions.ActionTypes;

/// <summary>
///     Instantaneous action with no extra targeting information. Will result in <see cref="PerformActionEvent"/> being raised.
/// </summary>
[Serializable, NetSerializable]
[Friend(typeof(SharedActionsSystem))]
[Virtual]
public class InstantAction : ActionType
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public PerformActionEvent? Event;

    public InstantAction() { }
    public InstantAction(InstantAction toClone)
    {
        CopyFrom(toClone);
    }

    public override void CopyFrom(object objectToClone)
    {
        base.CopyFrom(objectToClone);

        if (objectToClone is not InstantAction toClone)
            return;

        // Events should be re-usable, and shouldn't be modified during prediction.
        Event = toClone.Event;
    }

    public override object Clone()
    {
        return new InstantAction(this);
    }
}
