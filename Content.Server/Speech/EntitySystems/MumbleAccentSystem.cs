using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class MumbleAccentSystem : RelayAccentSystem<MumbleAccentComponent>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MumbleAccentComponent, EmoteEvent>(OnEmote, before: [typeof(VocalSystem)]);
    }

    // TODO: This likely will not hold up with a relay system!
    private void OnEmote(Entity<MumbleAccentComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        if (TryComp<VocalComponent>(ent.Owner, out var vocalComp) && vocalComp.EmoteSounds is { } sounds)
        {
            // play a muffled version of the vocal emote
            args.Handled = _chat.TryPlayEmoteSound(
                ent.Owner,
                ProtoMan.Index(sounds),
                args.Emote,
                ent.Comp.EmoteAudioParams);
        }
    }

    public override string Accentuate(string message, Entity<MumbleAccentComponent>? _)
    {
        return _replacement.ApplyReplacements(message, "mumble");
    }
}
