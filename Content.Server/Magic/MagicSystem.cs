using Content.Server.Chat.Systems;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Server.Lightning;

namespace Content.Server.Magic;

public sealed class MagicSystem : SharedMagicSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakSpellEvent>(OnSpellSpoken);
        SubscribeLocalEvent<LightningSpellEvent>(OnLightningSpell);
    }

    private void OnSpellSpoken(ref SpeakSpellEvent args)
    {
        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(args.Speech), InGameICChatType.Speak, false);
    }

    private void OnLightningSpell(LightningSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;
        Speak(ev);

        _lightning.ShootLightning(ev.Performer, ev.Target, ev.LightningPrototype);
    }
}
