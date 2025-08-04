using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent]
public sealed partial class InstantActionComponent : BaseActionComponent
{
    public override BaseActionEvent? BaseEvent => Event;

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public InstantActionEvent? Event;
}

[Serializable, NetSerializable]
public sealed class InstantActionComponentState : BaseActionComponentState
{
    public InstantActionComponentState(InstantActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
    }
}
