using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class MumbleAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MumbleAccentComponent, AccentGetEvent>(OnAccentGet);
        SubscribeLocalEvent<MumbleAccentComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem) });
    }

    private void OnEmote(EntityUid uid, MumbleAccentComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        if (TryComp<VocalComponent>(uid, out var vocalComp))
        {
            // play a muffled version of the vocal emote
            args.Handled = _chat.TryPlayEmoteSound(uid, vocalComp.EmoteSounds, args.Emote, component.EmoteAudioParams);
        }
    }

    public string Accentuate(string message, MumbleAccentComponent component)
    {
        return _replacement.ApplyReplacements(message, "mumble");
    }

    private void OnAccentGet(EntityUid uid, MumbleAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
