using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;

namespace Content.Shared.Speech.Muting;

/// <summary>
/// Handles the speech restrictions imposed by <see cref="MutedStatusEffectComponent"/>.
/// </summary>
public sealed partial class MutedStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnEmote(Entity<MutedStatusEffectComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        // Still leaves the text so it looks like they are pantomiming a laugh.
        if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
        {
            args.Handled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnEmoteAction(Entity<MutedStatusEffectComponent> ent, ref EmoteActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        _popup.PopupEntity(Loc.GetString(ent.Comp.ActionPopup), user, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSpeakAttempt(Entity<MutedStatusEffectComponent> ent, ref SpeakAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var target = args.Uid;
        _popup.PopupEntity(Loc.GetString(ent.Comp.SpeakPopup), target, target);
        args.Cancel();
    }
}
