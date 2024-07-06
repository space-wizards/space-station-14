using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

public sealed partial class EntityWorldTargetActionComponent : BaseTargetActionComponent
{
    public override BaseActionEvent? BaseEvent => Event;

    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public EntityWorldTargetActionEvent? Event;

    [DataField("whitelist")] public EntityWhitelist? Whitelist;

    [DataField("canTargetSelf")] public bool CanTargetSelf = true;
}

[Serializable, NetSerializable]
public sealed class EntityWorldTargetActionComponentState : BaseActionComponentState
{
    public EntityWhitelist? Whitelist;
    public bool CanTargetSelf;

    public EntityWorldTargetActionComponentState(EntityTargetActionComponent component, IEntityManager entManager) : base(component, entManager)
    {
        Whitelist = component.Whitelist;
        CanTargetSelf = component.CanTargetSelf;
    }
}
