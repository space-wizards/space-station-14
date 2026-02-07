using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.Clothing.Systems;
using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Clumsy;
using Content.Shared.Cluwne;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Cluwne;

public sealed class CluwneSystem : EntitySystem
{

    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
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
    private void OnMobState(Entity<CluwneComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<CluwneComponent>(ent.Owner);
            RemComp<ClumsyComponent>(ent.Owner);
            RemComp<AutoEmoteComponent>(ent.Owner);
            _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.RevertDamage);
        }
    }

    public EmoteSoundsPrototype? EmoteSounds;

    /// <summary>
    /// OnStartup gives the cluwne outfit, ensures clumsy, and makes sure emote sounds are laugh.
    /// </summary>
    private void OnComponentStartup(Entity<CluwneComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.EmoteSoundsId == null)
            return;

        _prototypeManager.TryIndex(ent.Comp.EmoteSoundsId, out EmoteSounds);


        if (ent.Comp.RandomEmote && ent.Comp.AutoEmoteId != null)
        {
            EnsureComp<AutoEmoteComponent>(ent.Owner);
            _autoEmote.AddEmote(ent.Owner, ent.Comp.AutoEmoteId);
        }

        EnsureComp<ClumsyComponent>(ent.Owner);

        var transformMessage = Loc.GetString(ent.Comp.TransformMessage, ("target", ent.Owner));

        _popupSystem.PopupEntity(transformMessage, ent.Owner, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.SpawnSound, ent.Owner);

        _nameMod.RefreshNameModifiers(ent.Owner);


        _outfitSystem.SetOutfit(ent.Owner, ent.Comp.OutfitId, unremovable: true);
    }

    /// <summary>
    /// Handles the timing on autoemote as well as falling over and honking.
    /// </summary>
    private void OnEmote(Entity<CluwneComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.RandomEmote)
            return;

        args.Handled = _chat.TryPlayEmoteSound(ent.Owner, EmoteSounds, args.Emote);

        if (_robustRandom.Prob(ent.Comp.GiggleRandomChance))
        {
            _audio.PlayPvs(ent.Comp.SpawnSound, ent.Owner);
            _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString(ent.Comp.GiggleEmote), InGameICChatType.Emote, ChatTransmitRange.Normal);
        }

        else if (_robustRandom.Prob(ent.Comp.KnockChance))
        {
            _audio.PlayPvs(ent.Comp.KnockSound, ent.Owner);
            _stunSystem.TryUpdateParalyzeDuration(ent.Owner, TimeSpan.FromSeconds(ent.Comp.ParalyzeTime));
            _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString(ent.Comp.KnockEmote), InGameICChatType.Emote, ChatTransmitRange.Normal);
        }
    }

    /// <summary>
    /// Applies "Cluwnified" prefix
    /// </summary>
    private void OnRefreshNameModifiers(Entity<CluwneComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier(ent.Comp.NamePrefix);
    }
}
