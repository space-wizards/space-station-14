using Content.Shared.Mobs;
using Content.Shared.Stealth.Components;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Cluwne;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Components;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.IdentityManagement;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Cluwne;

public sealed class CluwneBeastSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneBeastComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CluwneBeastComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<CluwneBeastComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneBeastComponent, MindAddedMessage>(OnCluwneBeastMindAdded);
        SubscribeLocalEvent<CluwneBeastComponent, EmoteEvent>(OnEmote, before:
        new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
    }

    /// <summary>
    /// On death removes autoemote.
    /// </summary>
    private void OnMobState(EntityUid uid, CluwneBeastComponent component, MobStateChangedEvent args)
    {

        if (args.NewMobState == MobState.Dead || args.NewMobState == MobState.Critical)
        {
            RemComp<AutoEmoteComponent>(uid);
            RemComp<StealthOnMoveComponent>(uid);
        }

        else
        {
            EnsureComp<AutoEmoteComponent>(uid);
            _autoEmote.AddEmote(uid, "CluwneGiggle");
            EnsureComp<StealthOnMoveComponent>(uid);
        }
    }

    /// <summary>
    /// OnStartup gives autoemote, makes cluwne beast clumsy and spawns a green portal.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, CluwneBeastComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;

        _prototypeManager.TryIndex(component.EmoteSoundsId, out component.EmoteSounds);
        EnsureComp<AutoEmoteComponent>(uid);
        _autoEmote.AddEmote(uid, "CluwneBeastGiggle");
        EnsureComp<ClumsyComponent>(uid);
        Spawn(component.BlueSpaceId, Transform(uid).Coordinates);
    }

    public CluwneBeastRuleConfiguration RuleConfig()
    {
        return (CluwneBeastRuleConfiguration) _prototypeManager.Index<GameRulePrototype>("CluwneBeastSpawn").Configuration;
    }

    private void OnCluwneBeastMindAdded(EntityUid uid, CluwneBeastComponent comp, MindAddedMessage args)
    {
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            HelloBeast(mind.Mind);
    }

    private void HelloBeast(Mind.Mind mind)
    {
        if (!mind.TryGetSession(out var session))
            return;

        var config = RuleConfig();
        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
        _chatMan.DispatchServerMessage(session, Loc.GetString("cluwne-beast-greeting"));
    }
    /// <summary>
    /// Handles the timing on autoemote as well as falling over and honking.
    /// </summary>
    private void OnEmote(EntityUid uid, CluwneBeastComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = _chat.TryPlayEmoteSound(uid, component.EmoteSounds, args.Emote);

        if (_robustRandom.Prob(component.GiggleRandomChance))
        {
            _audio.PlayPvs(component.SpawnSound, uid);
            _chat.TrySendInGameICMessage(uid, "honks", InGameICChatType.Emote, false, false);
        }

        else if (_robustRandom.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _chat.TrySendInGameICMessage(uid, "spasms", InGameICChatType.Emote, false, false);
        }
    }

    private void OnMeleeHit(EntityUid uid, CluwneBeastComponent component, MeleeHitEvent args)
    {
        foreach (var entity in args.HitEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(entity)
                && !_mobStateSystem.IsDead(entity)
                && _robustRandom.Prob(component.Cluwinification))
            {
                _audio.PlayPvs(component.CluwneSound, uid);
                EnsureComp<CluwneComponent>(entity);
            }
        }
    }
}

