using Content.Shared.CCVar;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Clumsy;

public sealed class ClumsySystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeHyposprayInjectsEvent>(BeforeHyposprayEvent);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeDefibrillatorZapsEvent>(BeforeDefibrillatorZapsEvent);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeGunShotEvent>(BeforeGunShotEvent);
        SubscribeLocalEvent<ClumsyComponent, CatchAttemptEvent>(OnCatchAttempt);
        SubscribeLocalEvent<ClumsyComponent, SelfBeforeClimbEvent>(OnBeforeClimbEvent);
    }

    // If you add more clumsy interactions add them in this section!
    #region Clumsy interaction events
    private void BeforeHyposprayEvent(Entity<ClumsyComponent> ent, ref SelfBeforeHyposprayInjectsEvent args)
    {
        // Clumsy people sometimes inject themselves! Apparently syringes are clumsy proof...

        // checks if ClumsyHypo is false, if so, skips.
        if (!ent.Comp.ClumsyHypo)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.TargetGettingInjected = args.EntityUsingHypospray;
        args.InjectMessageOverride = Loc.GetString(ent.Comp.HypoFailedMessage);
        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, args.EntityUsingHypospray);
    }

    private void BeforeDefibrillatorZapsEvent(Entity<ClumsyComponent> ent, ref SelfBeforeDefibrillatorZapsEvent args)
    {
        // Clumsy people sometimes defib themselves!

        // checks if ClumsyDefib is false, if so, skips.
        if (!ent.Comp.ClumsyDefib)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.DefibTarget = args.EntityUsingDefib;
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

    }

    private void OnCatchAttempt(Entity<ClumsyComponent> ent, ref CatchAttemptEvent args)
    {
        // Clumsy people sometimes fail to catch items!

        // checks if ClumsyCatching is false, if so, skips.
        if (!ent.Comp.ClumsyCatching)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(args.Item).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.Cancelled = true; // fail to catch

        if (ent.Comp.CatchingFailDamage != null)
            _damageable.ChangeDamage(ent.Owner, ent.Comp.CatchingFailDamage, origin: args.Item);

        // Collisions don't work properly with PopupPredicted or PlayPredicted.
        // So we make this server only.
        if (_net.IsClient)
            return;

        var selfMessage = Loc.GetString(ent.Comp.CatchingFailedMessageSelf, ("item", ent.Owner), ("catcher", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString(ent.Comp.CatchingFailedMessageOthers, ("item", ent.Owner), ("catcher", Identity.Entity(ent.Owner, EntityManager)));
        _popup.PopupEntity(selfMessage, ent.Owner, ent.Owner);
        _popup.PopupEntity(othersMessage, ent.Owner, Filter.PvsExcept(ent.Owner), true);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);
    }

    private void BeforeGunShotEvent(Entity<ClumsyComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        // Clumsy people sometimes can't shoot :(

        // checks if ClumsyGuns is false, if so, skips.
        if (!ent.Comp.ClumsyGuns)
            return;

        if (args.Gun.Comp.ClumsyProof)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(args.Gun).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        if (ent.Comp.GunShootFailDamage != null)
            _damageable.ChangeDamage(ent.Owner, ent.Comp.GunShootFailDamage, origin: ent);

        _stun.TryUpdateParalyzeDuration(ent, ent.Comp.GunShootFailStunTime);

        // Apply salt to the wound ("Honk!") (No idea what this comment means)
        _audio.PlayPvs(ent.Comp.GunShootFailSound, ent);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        _popup.PopupEntity(Loc.GetString(ent.Comp.GunFailedMessage), ent, ent);
        args.Cancel();
    }

    private void OnBeforeClimbEvent(Entity<ClumsyComponent> ent, ref SelfBeforeClimbEvent args)
    {
        // checks if ClumsyVaulting is false, if so, skips.
        if (!ent.Comp.ClumsyVaulting)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!_cfg.GetCVar(CCVars.GameTableBonk) && !rand.Prob(ent.Comp.ClumsyDefaultCheck))
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
                Loc.GetString(ent.Comp.VaulingFailedMessageSelf, ("bonkable", args.BeingClimbedOn)),
                Loc.GetString(ent.Comp.VaulingFailedMessageOthers, ("victim", gettingPutOnTableName), ("bonkable", args.BeingClimbedOn)),
                ent,
                ent);
        }
        else
        {
            // Someone else slamed you onto the table.
            // This is only run in server so you need to use popup entity.
            _popup.PopupPredicted(
                Loc.GetString(ent.Comp.VaulingFailedMessageForced,
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
                _damageable.ChangeDamage(target.Owner, bonkComp.BonkDamage, true);
        }

        _stun.TryUpdateParalyzeDuration(target, stunTime);
    }
    #endregion
}
