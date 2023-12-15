using Content.Server.Actions;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.UserInterface;

public sealed class IntrinsicUISystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntrinsicUIComponent, MapInitEvent>(InitActions);
        SubscribeLocalEvent<IntrinsicUIComponent, ToggleIntrinsicUIEvent>(OnActionToggle);
    }

    private void OnActionToggle(EntityUid uid, IntrinsicUIComponent component, ToggleIntrinsicUIEvent args)
    {
        args.Handled = InteractUI(uid, args.Key, component);
    }

    private void InitActions(EntityUid uid, IntrinsicUIComponent component, MapInitEvent args)
    {
        foreach (var entry in component.UIs)
        {
            _actionsSystem.AddAction(uid, ref entry.ToggleActionEntity, entry.ToggleAction);
        }
    }

    public bool InteractUI(EntityUid uid, Enum? key, IntrinsicUIComponent? iui = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref iui, ref actor))
            return false;

        if (key is null)
        {
            Log.Error($"Entity {ToPrettyString(uid)} has an invalid intrinsic UI.");
        }

        var ui = GetUIOrNull(uid, key, iui);

        if (ui is null)
        {
            Log.Error($"Couldn't get UI {key} on {ToPrettyString(uid)}");
            return false;
        }

        var attempt = new IntrinsicUIOpenAttemptEvent(uid, key);
        RaiseLocalEvent(uid, attempt);
        if (attempt.Cancelled)
            return false;

        _uiSystem.ToggleUi(ui, actor.PlayerSession);
        return true;
    }

    private PlayerBoundUserInterface? GetUIOrNull(EntityUid uid, Enum? key, IntrinsicUIComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return key is null ? null : _uiSystem.GetUiOrNull(uid, key);
    }
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
