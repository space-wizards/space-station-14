using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;

public abstract partial class SharedKodepiiaConsumeSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaConsumeActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<KodepiiaConsumeActionComponent, ComponentShutdown>(OnShutdown);
    }

    public sealed partial class KodepiiaConsumeEvent : EntityTargetActionEvent;
    [Serializable, NetSerializable]
    public sealed partial class KodepiiaConsumeDoAfterEvent : SimpleDoAfterEvent;

    public void OnShutdown(Entity<KodepiiaConsumeActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ConsumeAction);
    }

    public void OnStartup(Entity<KodepiiaConsumeActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ConsumeAction, ent.Comp.ConsumeActionId);
    }
}
