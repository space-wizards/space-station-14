using Content.Server.Administration.Commands;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Interaction.Components;
using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Emoting;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Server.Database;
using Content.Shared.Cluwne;
using Content.Shared.Interaction.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server.Cluwne;

public sealed class CluwneSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmoteSystem _emote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CluwneComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneComponent, EmoteEvent>(OnEmote, before:
            new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
    }

    public override void

     Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var cluwnecomp in EntityQuery<CluwneComponent>())
        {
            cluwnecomp.LastGiggleCooldown -= frameTime;
            RandomEmote(cluwnecomp.Owner, cluwnecomp);
        }
    }

    /// <summary>
    /// Cluwne will cluwne laugh when emoting.
    /// </summary>
    public EmoteSoundsPrototype? EmoteSounds;

    private void OnComponentStartup(EntityUid uid, CluwneComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;
        _prototypeManager.TryIndex(component.EmoteSoundsId, out EmoteSounds);

        var meta = MetaData(uid);
        var name = meta.EntityName;

        EnsureComp<ClumsyComponent>(uid);

        _popupSystem.PopupEntity(Loc.GetString("cluwne-transform", ("target", uid)), uid, PopupType.LargeCaution);
        _audio.PlayPvs(component.SpawnSound, uid);

        meta.EntityName = Loc.GetString("cluwne-name-prefix", ("target", name));

        SetOutfitCommand.SetOutfit(uid, "CluwneGear", EntityManager);
    }

    /// <summary>
    /// Makes sure the cluwne emits a cluwnelaugh on an emote that has a sound such as lol.
    /// </summary>
    private void OnEmote(EntityUid uid, CluwneComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = _chat.TryPlayEmoteSound(uid, EmoteSounds, args.Emote);
    }

    /// <summary>
    /// If cluwne is dead then the cluwne mechanic gets removed.
    /// </summary>
    private void OnMobState(EntityUid uid, CluwneComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
    
            RemComp<CluwneComponent>(uid);
            var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 300);
            _damageableSystem.TryChangeDamage(uid, damageSpec);
    }

    /// <summary>
    /// Makes cluwne do a random emote. Falldown and horn, twitch and honk, giggle.
    /// </summary>
    private void RandomEmote(EntityUid uid, CluwneComponent component)
    {
        if (component.LastGiggleCooldown > 0)
            return;

        if (_robustRandom.Prob(component.KnockChance))
        {
            _audio.PlayPvs(component.KnockSound, uid);
            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            _chat.TrySendInGameICMessage(uid, "spasms", InGameICChatType.Emote, false, false);
        }

        else if (_robustRandom.Prob(component.GiggleRandomChance))
             _chat.TrySendInGameICMessage(uid, "giggles", InGameICChatType.Emote, false, false);

        else if (_robustRandom.Prob(component.GiggleRandomChance))
        {
            _audio.PlayPvs(component.SpawnSound, uid);
            _chat.TrySendInGameICMessage(uid, "honks", InGameICChatType.Emote, false, false);
        }

        else
            component.LastGiggleCooldown = component.GiggleCooldown;
    }
}

