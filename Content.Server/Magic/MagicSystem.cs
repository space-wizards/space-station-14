using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Shared.Mind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

public sealed class MagicSystem : SharedMagicSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<TagPrototype> InvalidForSurvivorAntagTag = "InvalidForSurvivorAntag";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakSpellEvent>(OnSpellSpoken);
    }

    private void OnSpellSpoken(ref SpeakSpellEvent args)
    {
        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(args.Speech), InGameICChatType.Speak, false);
    }

    public override void OnVoidApplause(VoidApplauseSpellEvent ev)
    {
        base.OnVoidApplause(ev);

        _chat.TryEmoteWithChat(ev.Performer, ev.Emote);

        var perfXForm = Transform(ev.Performer);
        var targetXForm = Transform(ev.Target);

        Spawn(ev.Effect, perfXForm.Coordinates);
        Spawn(ev.Effect, targetXForm.Coordinates);
    }

    protected override void OnRandomGlobalSpawnSpell(RandomGlobalSpawnSpellEvent ev)
    {
        base.OnRandomGlobalSpawnSpell(ev);

        if (!ev.MakeSurvivorAntagonist)
            return;

        if (_mind.TryGetMind(ev.Performer, out var mind, out _) && !_tag.HasTag(mind, InvalidForSurvivorAntagTag))
            _tag.AddTag(mind, InvalidForSurvivorAntagTag);

        EntProtoId survivorRule = "Survivor";

        if (!_gameTicker.IsGameRuleActive<SurvivorRuleComponent>())
            _gameTicker.StartGameRule(survivorRule);
    }
}
