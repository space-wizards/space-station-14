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
        if (args.Key == null)
            return;

        args.Handled = InteractUI(uid, args.Key, component);
    }

    private void InitActions(EntityUid uid, IntrinsicUIComponent component, MapInitEvent args)
    {
        foreach (var entry in component.UIs.Values)
        {
            _actionsSystem.AddAction(uid, ref entry.ToggleActionEntity, entry.ToggleAction);
        }
    }

    public bool InteractUI(EntityUid uid, Enum key, IntrinsicUIComponent? iui = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref iui, ref actor))
            return false;

        var attempt = new IntrinsicUIOpenAttemptEvent(uid, key);
        RaiseLocalEvent(uid, attempt);
        if (attempt.Cancelled)
            return false;

        return _uiSystem.TryToggleUi(uid, key, actor.PlayerSession);
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
