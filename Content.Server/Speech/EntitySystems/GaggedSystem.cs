using Content.Server.Abilities.Mime;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Puppet;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;

namespace Content.Server.Speech;

public sealed class GaggedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GaggedComponent, EmoteEvent>(OnEmote, before: [typeof(VocalSystem)]);
        SubscribeLocalEvent<GaggedComponent, ScreamActionEvent>(OnScreamAction, before: [typeof(VocalSystem)]);
    }

    private void OnEmote(EntityUid uid, GaggedComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        // Suppresses the audio from vocalization emotes, but preserves the emote text.
        if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
        {
            args.Handled = true;
        }
    }

    private void OnScreamAction(EntityUid uid, GaggedComponent component, ScreamActionEvent args)
    {
        if (args.Handled)
        {
            return;

        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
        }
        args.Handled = true;
    }
}
