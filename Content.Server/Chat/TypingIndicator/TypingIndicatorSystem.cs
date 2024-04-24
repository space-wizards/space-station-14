using Content.Shared.ActionBlocker;
using Content.Shared.Chat.TypingIndicator;
using Robust.Shared.Player;

namespace Content.Server.Chat.TypingIndicator;

// Server-side typing system
// It receives networked typing events from clients
// And sync typing indicator using appearance component
public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TypingIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<TypingChangedEvent>(OnClientTypingChanged);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        // when player poses entity we want to make sure that there is typing indicator
        EnsureComp<TypingIndicatorComponent>(ev.Entity);
        // we also need appearance component to sync visual state
        EnsureComp<AppearanceComponent>(ev.Entity);
    }

    private void OnPlayerDetached(EntityUid uid, TypingIndicatorComponent component, PlayerDetachedEvent args)
    {
        // player left entity body - hide typing indicator
        SetTypingIndicatorEnabled(uid, false);
    }

    private void OnClientTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;
        if (!Exists(uid))
        {
            Log.Warning($"Client {args.SenderSession} sent TypingChangedEvent without an attached entity.");
            return;
        }

        // check if this entity can speak or emote
        if (!_actionBlocker.CanEmote(uid.Value) && !_actionBlocker.CanSpeak(uid.Value))
        {
            // nah, make sure that typing indicator is disabled
            SetTypingIndicatorEnabled(uid.Value, false);
            return;
        }

        SetTypingIndicatorEnabled(uid.Value, ev.IsTyping);
    }

    private void SetTypingIndicatorEnabled(EntityUid uid, bool isEnabled, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, isEnabled, appearance);
    }
}
