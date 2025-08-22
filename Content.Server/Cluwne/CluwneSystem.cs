using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Clothing.Systems;
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
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Clumsy;
using Content.Shared.Chat; // Starlight

namespace Content.Server.Cluwne;

public sealed class CluwneSystem : EntitySystem
{
    private static readonly ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";

    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    [Dependency] private readonly OutfitSystem _outfitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CluwneComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneComponent, EmoteEvent>(OnEmote, before:
        new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
        SubscribeLocalEvent<CluwneComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    private void OnMobState(EntityUid uid, CluwneComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<CluwneComponent>(uid);
            RemComp<ClumsyComponent>(uid);
            RemComp<AutoEmoteComponent>(uid);
            var damageSpec = new DamageSpecifier(_prototypeManager.Index(GeneticDamageGroup), 300);
            _damageableSystem.TryChangeDamage(uid, damageSpec);
        }
    }

    public EmoteSoundsPrototype? EmoteSounds;

    /// <summary>
    /// OnStartup gives the cluwne outfit, ensures clumsy, and makes sure emote sounds are laugh.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, CluwneComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;
        _prototypeManager.TryIndex(component.EmoteSoundsId, out EmoteSounds);

        var meta = MetaData(uid);
        var name = meta.EntityName;

        EnsureComp<AutoEmoteComponent>(uid);
        _autoEmote.AddEmote(uid, component.AutoEmoteSound);
        EnsureComp<ClumsyComponent>(uid);

        #region Starlight
        if (component.IsCluwne)
        {
            _popupSystem.PopupEntity(Loc.GetString("cluwne-transform", ("target", uid)), uid, PopupType.LargeCaution);
            _audio.PlayPvs(component.SpawnSound, uid);

            _outfitSystem.SetOutfit(uid, "CluwneGear");
            _faction.RemoveFaction(uid, "NanoTrasen", false);
            _faction.AddFaction(uid, "HonkNeutral");
        }
        else
        {
            Spawn(component.Portal, Transform(uid).Coordinates);
            _outfitSystem.SetOutfit(uid, "CluwneBeastGear");
            _audio.PlayPvs(component.ArrivalSound, uid);
        }
        _nameMod.RefreshNameModifiers(uid);
        #endregion Starlight
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
            _stunSystem.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(component.ParalyzeTime));
            _chat.TrySendInGameICMessage(uid, "spasms", InGameICChatType.Emote, ChatTransmitRange.Normal);
        }
    }

    /// <summary>
    /// Applies "Cluwnified" prefix
    /// </summary>
    private void OnRefreshNameModifiers(Entity<CluwneComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("cluwne-name-prefix");
    }
}
