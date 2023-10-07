using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.UserInterface;
using Content.Shared.Disposal.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Systems;

/// <summary>
/// Handles routing and UI logic for disposal routers.
/// UI is handled by DisposalTubeSystem.
/// </summary>
public sealed class DisposalRouterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalRouterComponent, ComponentRemove>(OnComponentRemove);
        // must override the junction logic
        SubscribeLocalEvent<DisposalRouterComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(DisposalTubeSystem) });
        // TODO: maybe this could use simple dialog thing that rename trick uses?
        SubscribeLocalEvent<DisposalRouterComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<DisposalRouterComponent, SharedDisposalRouterComponent.UiActionMessage>(OnUiAction);
    }

    private void OnComponentRemove(EntityUid uid, DisposalRouterComponent comp, ComponentRemove args)
    {
        _ui.TryCloseAll(uid, DisposalRouterUiKey.Key);
    }

    private void OnGetNextDirection(EntityUid uid, DisposalRouterComponent comp, ref GetDisposalsNextDirectionEvent args)
    {
        if (!args.Holder.Tags.Overlaps(comp.Tags))
        {
            args.Next = Transform(uid).LocalRotation.GetDir();
            return;
        }

        // use the junction side direction when a tag matches
        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(uid, ref ev);
        args.Next = ev.Connectable[1];
    }

    private void OnOpenUIAttempt(EntityUid uid, DisposalRouterComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        if (!TryComp<HandsComponent>(args.User, out var hands))
        {
            _popup.PopupEntity(Loc.GetString("disposal-router-window-tag-input-activate-no-hands"), uid, args.User);
            return;
        }

        var activeHandEntity = hands.ActiveHandEntity;
        if (activeHandEntity != null)
            args.Cancel();

        UpdateUserInterface(uid, comp);
    }

    private void UpdateUserInterface(EntityUid uid, DisposalRouterComponent comp)
    {
        var taglist = string.Join(", ", comp.Tags);
        var state = new DisposalRouterUserInterfaceState(taglist);
        _ui.TrySetUiState(uid, DisposalRouterUiKey.Key, state);
    }

    /// <summary>
    /// Handles ui messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnUiAction(EntityUid uid, DisposalRouterComponent router, SharedDisposalRouterComponent.UiActionMessage msg)
    {
        if (!DisposalRouterUiKey.Key.Equals(msg.UiKey))
            return;

        // this seems both unneccessary and too little
        // if its supposed to be validation why doesnt it check if user has hands and is in range, if a ui is open, etc...
        if (!Exists(msg.Session.AttachedEntity))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        //Check for correct message and ignore maleformed strings
        if (msg.Action == SharedDisposalRouterComponent.UiAction.Ok && SharedDisposalRouterComponent.TagRegex.IsMatch(msg.Tags))
        {
            router.Tags.Clear();
            foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                router.Tags.Add(tag.Trim());
                _audio.PlayPvs(router.ClickSound, uid);
            }
        }
    }
}
