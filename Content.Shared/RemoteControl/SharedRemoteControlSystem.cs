using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.RemoteControl.Components;

namespace Content.Shared.RemoteControl;

/// <summary>
/// guh
/// </summary>
public abstract partial class SharedRemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemotelyControllableComponent, ComponentInit>(OnControllableInit);
        SubscribeLocalEvent<RemotelyControllableComponent, ComponentShutdown>(OnControllableShutdown);
        SubscribeLocalEvent<RemotelyControllableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<RemotelyControllableComponent> ent, ref ExaminedEvent args)
    {
        /*if (ent.Comp.ExamineMessage == null)
            return;*/

        if (!args.IsInDetailsRange)
            return;

        args.PushText("It has an antenna attached to it.");
    }
    private void OnControllableInit(Entity<RemotelyControllableComponent> ent, ref ComponentInit args)
    {
        EntityUid? actionEnt = null;
        _actions.AddAction(ent.Owner, ref actionEnt, "ActionRCBackToBody");
        if (actionEnt != null)
            ent.Comp.ReturnAbility = actionEnt.Value;
    }

    private void OnControllableShutdown(Entity<RemotelyControllableComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ReturnAbility);
    }
}

public sealed partial class RCReturnToBodyEvent : InstantActionEvent
{

}
