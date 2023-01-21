using Content.Server.Administration.Commands;
using Content.Server.Speech.Components;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Interaction.Components;
using Content.Shared.Mobs;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<CluwneComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnMobState(EntityUid uid, CluwneComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<CluwneComponent>(uid);
            var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 300);
            _damageableSystem.TryChangeDamage(uid, damageSpec);
        }
    }
    /// <summary>
    /// Gives target cluwne scream, backwards accent, makes clumsy, inserts a popup message and gives cluwne clothing.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, CluwneComponent component, ComponentStartup args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;

        EnsureComp<BackwardsAccentComponent>(uid);

        var vocal = EnsureComp<VocalComponent>(uid);
        var scream = new SoundCollectionSpecifier("CluwneScreams");
        vocal.FemaleScream = scream;
        vocal.MaleScream = scream;

        EnsureComp<ClumsyComponent>(uid);

        _popupSystem.PopupEntity(Loc.GetString("cluwne-transform", ("target", uid)), uid, PopupType.LargeCaution);
        _audio.PlayPvs(component.SpawnSound, uid);

        meta.EntityName = Loc.GetString("cluwne-name-prefix", ("target", name));

        SetOutfitCommand.SetOutfit(uid, "CluwneGear", EntityManager);
    }
    /// <summary>
    /// Makes cluwne do a random emote. Falldown and horn, twitch and honk, giggle.
    /// </summary>
    private void DoGiggle(EntityUid uid, CluwneComponent component)
    {
        if (component.LastGiggleCooldown > 0)
            return;

            if (_robustRandom.Prob(0.2f))
            { 
                _audio.PlayPvs(component.KnockSound, uid);
                _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            }

            if (_robustRandom.Prob(0.3f))
            {
                _audio.PlayPvs(component.Giggle, uid);
            }

            else
            {
                _popupSystem.PopupEntity(Loc.GetString("cluwne-twitch", ("target", Identity.Entity(uid, EntityManager))), uid);
                _audio.PlayPvs(component.SpawnSound, uid);
            }

        component.LastGiggleCooldown = component.GiggleCooldown;
         
    }

    public override void
         Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var cluwnecomp in EntityQuery<CluwneComponent>())
        {
            cluwnecomp.LastGiggleCooldown -= frameTime;


            if (_timing.CurTime <= cluwnecomp.GiggleGoChance)
                continue;

            cluwnecomp.GiggleGoChance += TimeSpan.FromSeconds(cluwnecomp.RandomGiggleAttempt);

            if (!_robustRandom.Prob(cluwnecomp.GiggleChance))
                continue;

            DoGiggle(cluwnecomp.Owner, cluwnecomp);
        }

    }

}

