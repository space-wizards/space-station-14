using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server.Speech.Muting;

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
        SubscribeLocalEvent<MutedStatusEffectComponent, StatusEffectRelayedEvent<EmoteEvent>>(OnEmote, before: new[] { typeof(VocalSystem), typeof(MumbleAccentSystem) });
        SubscribeLocalEvent<MutedStatusEffectComponent, StatusEffectRelayedEvent<ScreamActionEvent>>(OnScreamAction, before: new[] { typeof(VocalSystem) });
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

    private void OnScreamAction(Entity<MutedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ScreamActionEvent> args)
    {
        if (args.Args.Handled)
            return;

        if (!TryComp<StatusEffectComponent>(ent, out var statusEffect))
            return;

        if (statusEffect.AppliedTo is not { } target)
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.ScreamPopup), target, target);
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
