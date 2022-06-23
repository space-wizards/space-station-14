using Content.Shared.ActionBlocker;
using Content.Shared.Chat.TypingIndicator;
using Robust.Server.GameObjects;

namespace Content.Server.Chat.TypingIndicator;

// Server-side typing system
// It receives networked typing events from clients
// And sync typing indicator using appearance component
public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

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
        EnsureComp<ServerAppearanceComponent>(ev.Entity);
    }

    private void OnPlayerDetached(EntityUid uid, TypingIndicatorComponent component, PlayerDetachedEvent args)
    {
        // player left entity body - hide typing indicator
        SetTypingIndicatorEnabled(uid, false);
    }

    private void OnClientTypingChanged(TypingChangedEvent ev)
    {
        // make sure that this entity still exist
        if (!Exists(ev.Uid))
        {
            Logger.Warning($"Client attached entity {ev.Uid} from TypingChangedEvent doesn't exist on server.");
            return;
        }

        // check if this entity can speak or emote
        if (!_actionBlocker.CanEmote(ev.Uid) && !_actionBlocker.CanSpeak(ev.Uid))
        {
            // nah, make sure that typing indicator is disabled
            SetTypingIndicatorEnabled(ev.Uid, false);
            return;
        }

        SetTypingIndicatorEnabled(ev.Uid, ev.IsTyping);
    }

    private void SetTypingIndicatorEnabled(EntityUid uid, bool isEnabled, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        appearance.SetData(TypingIndicatorVisuals.IsTyping, isEnabled);
    }
}
