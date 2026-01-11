using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
///     Server-side class implementing the core UI logic of NanoTask
/// </summary>
public sealed class NanoTaskCartridgeSystem : SharedNanoTaskCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);

        SubscribeLocalEvent<NanoTaskInteractionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnCartridgeRemoved(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (!_cartridgeLoader.HasProgram<NanoTaskCartridgeComponent>(args.Loader))
        {
            RemComp<NanoTaskInteractionComponent>(args.Loader);
        }
    }

    private void OnInteractUsing(Entity<NanoTaskInteractionComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cartridgeLoader.TryGetProgram<NanoTaskCartridgeComponent>(ent.Owner, out var uid, out var program))
        {
            return;
        }
        if (!TryComp<NanoTaskPrintedComponent>(args.Used, out var printed))
        {
            return;
        }
        if (printed.Task is NanoTaskItem item)
        {
            program.Tasks.Add(new(program.Counter++, printed.Task));
            args.Handled = true;
            Del(args.Used);
            UpdateUiState(new Entity<NanoTaskCartridgeComponent>(uid.Value, program), ent.Owner);
        }
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void SetupPrintedTask(EntityUid uid, NanoTaskItem item)
    {
        PaperComponent? paper = null;
        NanoTaskPrintedComponent? printed = null;
        if (!Resolve(uid, ref paper, ref printed))
            return;

        printed.Task = item;
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("nano-task-printed-description", ("description", FormattedMessage.EscapeText(item.Description))));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("nano-task-printed-requester", ("requester", FormattedMessage.EscapeText(item.TaskIsFor))));
        msg.PushNewline();
        msg.AddMarkupOrThrow(item.Priority switch {
            NanoTaskPriority.High => Loc.GetString("nano-task-printed-high-priority"),
            NanoTaskPriority.Medium => Loc.GetString("nano-task-printed-medium-priority"),
            NanoTaskPriority.Low => Loc.GetString("nano-task-printed-low-priority"),
            _ => "",
        });

        _paper.SetContent((uid, paper), msg.ToMarkup());
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not NanoTaskUiMessageEvent message)
            return;

        switch (message.Payload)
        {
            case NanoTaskAddTask task:
                if (!task.Item.Validate())
                    return;

                ent.Comp.Tasks.Add(new(ent.Comp.Counter++, task.Item));
                break;
            case NanoTaskUpdateTask task:
            {
                if (!task.Item.Data.Validate())
                    return;

                var idx = ent.Comp.Tasks.FindIndex(t => t.Id == task.Item.Id);
                if (idx != -1)
                    ent.Comp.Tasks[idx] = task.Item;
                break;
            }
            case NanoTaskDeleteTask task:
                ent.Comp.Tasks.RemoveAll(t => t.Id == task.Id);
                break;
            case NanoTaskPrintTask task:
            {
                if (!task.Item.Validate())
                    return;
                if (_timing.CurTime < ent.Comp.NextPrintAllowedAfter)
                    return;

                ent.Comp.NextPrintAllowedAfter = _timing.CurTime + ent.Comp.PrintDelay;
                var printed = Spawn("PaperNanoTaskItem", Transform(message.Actor).Coordinates);
                _hands.PickupOrDrop(message.Actor, printed);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/printer.ogg"), ent.Owner);
                SetupPrintedTask(printed, task.Item);
                break;
            }
        }

        UpdateUiState(ent, GetEntity(args.LoaderUid));
    }


    private void UpdateUiState(Entity<NanoTaskCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new NanoTaskUiState(ent.Comp.Tasks);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }
}
