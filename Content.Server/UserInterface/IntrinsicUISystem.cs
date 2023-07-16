using Content.Server.Actions;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.UserInterface;

public sealed class IntrinsicUISystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntrinsicUIComponent, ComponentStartup>(OnGetActions);
        SubscribeLocalEvent<IntrinsicUIComponent, ToggleIntrinsicUIEvent>(OnActionToggle);
    }

    private void OnActionToggle(EntityUid uid, IntrinsicUIComponent component, ToggleIntrinsicUIEvent args)
    {
        args.Handled = InteractUI(uid, args.Key, component);
    }

    private void OnGetActions(EntityUid uid, IntrinsicUIComponent component, ComponentStartup args)
    {
        if (!TryComp<ActionsComponent>(uid, out var actions))
            return;

        foreach (var entry in component.UIs)
        {
            _actionsSystem.AddAction(uid, entry.ToggleAction, null, actions);
        }
    }

    public bool InteractUI(EntityUid uid, Enum? key, IntrinsicUIComponent? iui = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref iui, ref actor))
            return false;

        if (key is null)
        {
            Logger.ErrorS("bui", $"Entity {ToPrettyString(uid)} has an invalid intrinsic UI.");
        }

        var ui = GetUIOrNull(uid, key, iui);

        if (ui is null)
        {
            Logger.ErrorS("bui", $"Couldn't get UI {key} on {ToPrettyString(uid)}");
            return false;
        }

        var attempt = new IntrinsicUIOpenAttemptEvent(uid, key);
        RaiseLocalEvent(uid, attempt, false);
        if (attempt.Cancelled)
            return false;

        ui.Toggle(actor.PlayerSession);
        return true;
    }

    private BoundUserInterface? GetUIOrNull(EntityUid uid, Enum? key, IntrinsicUIComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return key is null ? null : uid.GetUIOrNull(key);
    }
}

[UsedImplicitly]
public sealed class ToggleIntrinsicUIEvent : InstantActionEvent
{
    [ViewVariables]
    public Enum? Key { get; set; }
}

// Competing with ActivatableUI for horrible event names.
public sealed class IntrinsicUIOpenAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public Enum? Key { get; }
    public IntrinsicUIOpenAttemptEvent(EntityUid who, Enum? key)
    {
        User = who;
        Key = key;
    }
}
