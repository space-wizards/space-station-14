using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

/// <summary>
/// Used on action entities to define an action that triggers when targeting an entity coordinate.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WorldTargetActionComponent : BaseTargetActionComponent
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
    public List<WorldTargetActionEvent> Events;
}

[Serializable, NetSerializable]
public sealed class WorldTargetActionComponentState : BaseActionComponentState
{
    public WorldTargetActionComponentState(WorldTargetActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
    }
}
