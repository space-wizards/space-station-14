using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

/// <summary>
/// Used on action entities to define an action that triggers when targeting an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityTargetActionComponent : BaseTargetActionComponent
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
    public List<EntityTargetActionEvent> Events;

    /// <summary>
    /// Determines which entities are valid targets for this action.
    /// </summary>
    /// <remarks>No whitelist check when null.</remarks>
    [DataField] public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whether this action considers the user as a valid target entity when using this action.
    /// </summary>
    [DataField] public bool CanTargetSelf = true;
}

[Serializable, NetSerializable]
public sealed class EntityTargetActionComponentState : BaseActionComponentState
{
    public EntityWhitelist? Whitelist;
    public bool CanTargetSelf;

    public EntityTargetActionComponentState(EntityTargetActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
        Whitelist = component.Whitelist;
        CanTargetSelf = component.CanTargetSelf;
    }
}
