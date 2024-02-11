using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityTargetActionComponent : BaseTargetActionComponent
{
    public override BaseActionEvent? BaseEvent => Event;

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public EntityTargetActionEvent? Event;

    [DataField("whitelist")] public EntityWhitelist? Whitelist;

    [DataField("canTargetSelf")] public bool CanTargetSelf = true;
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
