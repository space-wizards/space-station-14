using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Speech.Muting;

/// <summary>
/// Handles the speech restrictions imposed by <see cref="MutedStatusEffectComponent"/>.
/// </summary>
public sealed partial class MutedStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<MutedStatusEffectComponent, StatusEffectRelayedEvent<SpeakAttemptEvent>>(OnSpeakAttempt);
        SubscribeLocalEvent<MutedStatusEffectComponent, StatusEffectRelayedEvent<EmoteEvent>>(OnEmote);
        SubscribeLocalEvent<MutedStatusEffectComponent, StatusEffectRelayedEvent<EmoteActionEvent>>(OnEmoteAction);
    }

    private void OnEmote(Entity<MutedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<EmoteEvent> args)
    {
        if (args.Args.Handled)
            return;

        // Still leaves the text so it looks like they are pantomiming a laugh.
        if (args.Args.Emote.Category.HasFlag(EmoteCategory.Vocal))
        {
            args.Args = args.Args with { Handled = true };
        }
    }

    private void OnEmoteAction(Entity<MutedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<EmoteActionEvent> args)
    {
        if (args.Args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.ActionPopup), args.AppliedTo, args.AppliedTo);
        args.Args.Handled = true;
    }

    private void OnSpeakAttempt(Entity<MutedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SpeakAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        var target = args.Args.Uid;

        _popup.PopupEntity(Loc.GetString(ent.Comp.SpeakPopup), target, target);

        args.Args.Cancel();
    }
}
