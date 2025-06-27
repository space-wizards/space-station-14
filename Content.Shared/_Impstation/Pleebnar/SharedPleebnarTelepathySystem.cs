using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Pleebnar;

[Serializable, NetSerializable]
public enum PleebnarTelepathyUIKey : byte
{
    Key
}
/// <summary>
/// contains pleebnar telepathy relevant functions needed to be shared across clients and servers
/// </summary>

//message from server to client to determine the new state for the UI
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyBuiState : BoundUserInterfaceState
{
    public readonly string? Vision;

    public PleebnarTelepathyBuiState(string? vision)
    {
        Vision = vision;
    }
}

//message from client to server determined which contains the selected vision
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyVisionMessage : BoundUserInterfaceMessage
{
    public readonly string? Vision;

    public PleebnarTelepathyVisionMessage(string? vision)
    {
        Vision = vision;
    }
}

public abstract partial class SharedPleebnarTelepathySystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    //init
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentShutdown>(OnShutdown);
    }
    //event for selecting a receiver
    public sealed partial class PleebnarTelepathyEvent : EntityTargetActionEvent;
    //event for sending a vision after a delay
    [Serializable, NetSerializable]
    public sealed partial class PleebnarTelepathyDoAfterEvent : SimpleDoAfterEvent;
    //event for opening the ui
    public sealed partial class PleebnarVisionEvent : InstantActionEvent;

    //remove actions when component is removed
    public void OnShutdown(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.TelepathyAction);
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.VisionAction);

    }

    //add actions when component is added
    public void OnStartup(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.TelepathyAction, ent.Comp.TelepathyActionId);
        _actionsSystem.AddAction(ent, ref ent.Comp.VisionAction, ent.Comp.VisionActionId);
    }
}
