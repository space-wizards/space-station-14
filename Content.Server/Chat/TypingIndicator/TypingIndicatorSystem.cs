using Content.Shared.Chat.TypingIndicator;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Chat.TypingIndicator;

public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeNetworkEvent<TypingChangedEvent>(OnClientTypingChanged);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        // when player poses entity we want to add typing indicators
        // we also need appearance component to sync visual state
        EnsureComp<TypingIndicatorComponent>(ev.Entity);
        EnsureComp<ServerAppearanceComponent>(ev.Entity);
    }


    private void OnClientTypingChanged(TypingChangedEvent ev)
    {
        if (!Exists(ev.Uid))
        {
            Logger.Warning($"Client attached entity {ev.Uid} from TypingChangedEvent doesn't exist on server.");
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
