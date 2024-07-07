using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityWorldTargetActionComponent : BaseTargetActionComponent
{
    public override BaseActionEvent? BaseEvent => Event;

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField]
    [NonSerialized]
    public EntityWorldTargetActionEvent? Event;

    [DataField] public EntityWhitelist? Whitelist;

    [DataField] public bool CanTargetSelf = true;
}

[Serializable, NetSerializable]
public sealed class EntityWorldTargetActionComponentState(
    EntityWorldTargetActionComponent component,
    IEntityManager entManager)
    : BaseActionComponentState(component, entManager)
{
    public EntityWhitelist? Whitelist = component.Whitelist;
    public bool CanTargetSelf = component.CanTargetSelf;
}
