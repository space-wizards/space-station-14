using Content.Server.Administration.Commands;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Cluwne;
using Content.Shared.Interaction.Components;
using Content.Server.NPC.Systems;

namespace Content.Server.Cluwne;

public sealed class CluwneSystem : EntitySystem
{
    [Dependency] private readonly AutoEmoteSystem _emote = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CluwneComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneComponent, EmoteEvent>(OnEmote, before:
        new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
    }

    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    private void OnMobState(EntityUid uid, CluwneComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Genetic"), 300);
            _damageable.TryChangeDamage(uid, damageSpec);
            RemComp<ClumsyComponent>(uid);
            RemComp<AutoEmoteComponent>(uid);
            RemComp<CluwneComponent>(uid);
        }
    }

    public EmoteSoundsPrototype? EmoteSounds;

    /// <summary>
    /// OnStartup gives the cluwne outfit, ensures clumsy, gives name prefix and makes sure emote sounds are laugh.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, CluwneComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;
        _proto.TryIndex(component.EmoteSoundsId, out EmoteSounds);

        var meta = MetaData(uid);
        var name = meta.EntityName;

        EnsureComp<AutoEmoteComponent>(uid);
        _emote.AddEmote(uid, component.AutoEmoteSound);
        EnsureComp<ClumsyComponent>(uid);

        if (component.IsCluwne)
        {
            _popup.PopupEntity(Loc.GetString("cluwne-transform", ("target", uid)), uid, PopupType.LargeCaution);
            _audio.PlayPvs(component.SpawnSound, uid);
            _meta.SetEntityName(uid, Loc.GetString("cluwne-name-prefix", ("target", name)), meta);
            SetOutfitCommand.SetOutfit(uid, "CluwneGear", EntityManager);
            _faction.RemoveFaction(uid, "NanoTrasen", false);
            _faction.AddFaction(uid, "HonkNeutral");
        }

        else
        {
            Spawn(component.Portal, Transform(uid).Coordinates);
            SetOutfitCommand.SetOutfit(uid, "CluwneBeastGear", EntityManager);
            _audio.PlayPvs(component.ArrivalSound, uid);
        }
    }

    /// <summary>
    /// Handles the timing on autoemote as well as falling over and honking.
    /// </summary>
    private void OnEmote(EntityUid uid, CluwneComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = _chat.TryPlayEmoteSound(uid, EmoteSounds, args.Emote);

        if (_random.Prob(component.GiggleRandomChance))
        {
            _audio.PlayPvs(component.SpawnSound, uid);
            _chat.TrySendInGameICMessage(uid, "honks", InGameICChatType.Emote, ChatTransmitRange.Normal);
        }

        else if (_random.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stun.TryParalyze(uid, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _chat.TrySendInGameICMessage(uid, "spasms", InGameICChatType.Emote, ChatTransmitRange.Normal);
        }
    }
}
