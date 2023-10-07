using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.UserInterface;
using Content.Shared.Disposal;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Disposal.Tube.Systems;

/// <summary>
/// Handles routing and UI logic for disposal taggers.
/// </summary>
public sealed class DisposalTaggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalTaggerComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetNextDirection);
        // TODO: maybe this could use simple dialog thing that rename trick uses?
        SubscribeLocalEvent<DisposalTaggerComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<DisposalTaggerComponent, TaggerSetTagMessage>(OnSetTag);
    }

    private void OnComponentRemove(EntityUid uid, DisposalTaggerComponent comp, ComponentRemove args)
    {
        _ui.TryCloseAll(uid, DisposalTaggerUiKey.Key);
    }

    private void OnGetNextDirection(EntityUid uid, DisposalTaggerComponent comp, ref GetDisposalsNextDirectionEvent args)
    {
        args.Holder.Tags.Add(comp.Tag);
    }

    private void OnOpenUIAttempt(EntityUid uid, DisposalTaggerComponent comp, ActivatableUIOpenAttemptEvent args)
    {
        if (!TryComp<HandsComponent>(args.User, out var hands))
        {
            _popup.PopupEntity(Loc.GetString("disposal-Tagger-window-tag-input-activate-no-hands"), uid, args.User);
            return;
        }

        var activeHandEntity = hands.ActiveHandEntity;
        if (activeHandEntity != null)
            args.Cancel();

        UpdateUserInterface(uid, comp);
    }

    private void UpdateUserInterface(EntityUid uid, DisposalTaggerComponent comp)
    {
        var state = new DisposalTaggerUserInterfaceState(comp.Tag);
        _ui.TrySetUiState(uid, DisposalTaggerUiKey.Key, state);
    }

    /// <summary>
    /// Handles ui messages from the client. For things such as button presses
    /// which interact with the world and require server action.
    /// </summary>
    /// <param name="msg">A user interface message from the client.</param>
    private void OnSetTag(EntityUid uid, DisposalTaggerComponent tagger, TaggerSetTagMessage msg)
    {
        if (!DisposalTaggerUiKey.Key.Equals(msg.UiKey))
            return;

        // Ignore malformed strings
        if (!TaggerSetTagMessage.TagRegex.IsMatch(msg.Tag))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
            return;

        tagger.Tag = msg.Tag;
        _audio.PlayPvs(tagger.ClickSound, uid);
    }
}
