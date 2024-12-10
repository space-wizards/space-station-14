using Content.Shared.CCVar;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Clumsy;

public sealed class ClumsySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeHyposprayInjectsEvent>(BeforeHyposprayEvent);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeDefibrillatorZapsEvent>(BeforeDefibrillatorZapsEvent);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeGunShotEvent>(BeforeGunShotEvent);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeClimbEvent>(OnBeforeClimbEvent);
    }

    // If you add more clumsy interactions add them in this section!
    #region Clumsy interaction events
    private void BeforeHyposprayEvent(Entity<ClumsyComponent> ent, ref SelfBeforeHyposprayInjectsEvent args)
    {
        // Clumsy people sometimes inject themselves! Apparently syringes are clumsy proof...
        if (!_random.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.TargetGettingInjected = args.EntityUsingHypospray;
        args.InjectMessageOverride = "hypospray-component-inject-self-clumsy-message";
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);
    }

    private void BeforeDefibrillatorZapsEvent(Entity<ClumsyComponent> ent, ref SelfBeforeDefibrillatorZapsEvent args)
    {
        // Clumsy people sometimes defib themselves!
        if (!_random.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.DefibTarget = args.EntityUsingDefib;
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

    }

    private void BeforeGunShotEvent(Entity<ClumsyComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        // Clumsy people sometimes can't shoot :(

        if (args.Gun.Comp.ClumsyProof)
            return;

        if (!_random.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        if (ent.Comp.GunShootFailDamage != null)
            _damageable.TryChangeDamage(ent, ent.Comp.GunShootFailDamage, origin: ent);

        _stun.TryParalyze(ent, ent.Comp.GunShootFailStunTime, true);

        // Apply salt to the wound ("Honk!") (No idea what this comment means)
        _audio.PlayPvs(ent.Comp.GunShootFailSound, ent);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        _popup.PopupEntity(Loc.GetString("gun-clumsy"), ent, ent);
        args.Cancel();
    }

    private void OnBeforeClimbEvent(Entity<ClumsyComponent> ent, ref SelfBeforeClimbEvent args)
    {
        // This event is called in shared, thats why it has all the extra prediction stuff.
        var rand = new System.Random((int)_timing.CurTick.Value);

        // If someone is putting you on the table, always get past the guard.
        if (!_cfg.GetCVar(CCVars.GameTableBonk) && args.PuttingOnTable == ent.Owner && !rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        HitHeadClumsy(ent, args.BeingClimbedOn);

        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, ent);

        _audio.PlayPredicted(ent.Comp.TableBonkSound, ent, ent);

        var gettingPutOnTableName = Identity.Entity(args.GettingPutOnTable, EntityManager);
        var puttingOnTableName = Identity.Entity(args.PuttingOnTable, EntityManager);

        if (args.PuttingOnTable == ent.Owner)
        {
            // You are slamming yourself onto the table.
            _popup.PopupPredicted(
                Loc.GetString("bonkable-success-message-user", ("bonkable", args.BeingClimbedOn)),
                Loc.GetString("bonkable-success-message-others", ("victim", gettingPutOnTableName), ("bonkable", args.BeingClimbedOn)),
                ent,
                ent);
        }
        else
        {
            // Someone else slamed you onto the table.
            // This is only run in server so you need to use popup entity.
            _popup.PopupPredicted(
                Loc.GetString("forced-bonkable-success-message",
                    ("bonker", puttingOnTableName),
                    ("victim", gettingPutOnTableName),
                    ("bonkable", args.BeingClimbedOn)),
                ent,
                null);
        }

        args.Cancel();
    }
    #endregion

    #region Helper functions
    /// <summary>
    ///     "Hits" an entites head against the given table.
    /// </summary>
    // Oh this fucntion is public le- NO!! This is only public for the one admin command if you use this anywhere else I will cry.
    public void HitHeadClumsy(Entity<ClumsyComponent> target, EntityUid table)
    {
        var stunTime = target.Comp.ClumsyDefaultStunTime;

        if (TryComp<BonkableComponent>(table, out var bonkComp))
        {
            stunTime = bonkComp.BonkTime;
            if (bonkComp.BonkDamage != null)
                _damageable.TryChangeDamage(target, bonkComp.BonkDamage, true);
        }

        _stun.TryParalyze(target, stunTime, true);
    }
    #endregion
}
