using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent]
public sealed partial class InstantActionComponent : BaseActionComponent
{
    public override List<BaseActionEvent> BaseEvents
    {
        get
        {
            var list = new List<BaseActionEvent>();
            foreach (var ev in Events)
            {
                list.Add(ev);
            }

            return list;
        }
    }

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField]
    [NonSerialized]
    public List<InstantActionEvent> Events;
}

[Serializable, NetSerializable]
public sealed class InstantActionComponentState : BaseActionComponentState
{
    public InstantActionComponentState(InstantActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
    }
}
