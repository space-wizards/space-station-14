using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Eye.Blinking;

namespace Content.Server.Eye.Blinking;

/// <inheritdoc/>
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    // TODO: Move all this stuff out once chat is predicted.
    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        SendEmoteMessage(ent, args.Emote.ID);
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<EmoteEvent> args)
    {
        if (args.Args.Handled)
            return;

        args.Args = args.Args with { Handled = true };

        SendEmoteMessage(ent, args.Args.Emote.ID);
    }

    private void SendEmoteMessage(Entity<EyeBlinkingComponent> ent, string emoteId)
    {
        if (!ent.Comp.BlinkEmoteId.Contains(emoteId))
            return;

        if (ent.Comp.Status != BlinkStatus.Normal)
            return;

        var ev = new BlinkEyeEvent(GetNetEntity(ent.Owner));
        RaiseNetworkEvent(ev);
    }
}
