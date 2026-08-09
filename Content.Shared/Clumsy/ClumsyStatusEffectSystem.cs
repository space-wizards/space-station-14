using Content.Shared.CCVar;
using Content.Shared.Chemistry.Events;
using Content.Shared.Climbing.Events;
using Content.Shared.Climbing.Systems;
using Content.Shared.Clumsy.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Clumsy;

/// <summary>
/// Handles status effects which cause the afflicted to randomly fail certain events.
/// </summary>
public sealed partial class ClumsyStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private ClimbSystem _climb = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    #region Subscriptions

    // Clumsy people are bad at baseball!
    [SubscribeLocalEvent]
    private void OnCatchAttemptEvent(Entity<ClumsyCatchStatusEffectComponent> status, ref StatusEffectRelayedEvent<CatchAttemptEvent> args)
    {
        if (args.Args.Cancelled
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(args.AppliedTo)))
            return;

        var ev = args.Args;
        ev.Cancelled = true;

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(args.AppliedTo, status.Comp.FailDamage, origin: args.Args.Item);

        var identity = Identity.Entity(args.AppliedTo, EntityManager);

        var selfMessage = status.Comp.SelfFailedMessage == null
            ? null
            : Loc.GetString(status.Comp.SelfFailedMessage, ("item", args.Args.Item));
        var othersMessage = status.Comp.OtherFailedMessage == null
            ? null
            :  Loc.GetString(status.Comp.OtherFailedMessage, ("item", args.Args.Item), ("catcher", identity));

        _popup.PopupEntity(selfMessage, othersMessage, args.AppliedTo, args.AppliedTo);

        // _audio.PlayPredicted doesn't play nice with collision events so we need PlayPvs
        // exit early for clients so the sound doesn't play twice
        if (_net.IsClient)
            return;

        _audio.PlayPvs(status.Comp.ClumsySound, args.AppliedTo);
    }

    // Clumsy people shock themselves with defibrillators!
    [SubscribeLocalEvent]
    private void OnBeforeDefibrillatorZapsEvent(Entity<ClumsyDefibStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeDefibrillatorZapsEvent> args)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(args.AppliedTo)))
            return;

        var ev = args.Args;
        ev.DefibTarget = ev.EntityUsingDefib;

        if (status.Comp.FailedMessage != null)
            _popup.PopupEntity(Loc.GetString(status.Comp.FailedMessage), args.AppliedTo, args.AppliedTo);

        _audio.PlayPredicted(status.Comp.ClumsySound, args.AppliedTo, args.AppliedTo);
    }

    // Clumsy people can't be trusted with guns!
    [SubscribeLocalEvent]
    private void OnBeforeGunShotEvent(Entity<ClumsyGunStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeGunShotEvent> args)
    {
        if (args.Args.Cancelled
            || args.Args.Gun.Comp.ClumsyProof
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(args.AppliedTo)))
            return;

        args.Args.Cancel();

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(args.AppliedTo, status.Comp.FailDamage, origin: args.Args.Gun);

        _stun.TryUpdateParalyzeDuration(args.AppliedTo, status.Comp.StunDuration);

        if (status.Comp.FailedMessage != null)
            _popup.PopupEntity(Loc.GetString(status.Comp.FailedMessage, ("gun", args.Args.Gun)), args.AppliedTo, args.AppliedTo);

        // SelfBeforeGunShotEvent is raised on server so _audio.PlayPredicted fails to play locally
        if (_net.IsClient)
            return;

        // Apply salt to the wound ("Honk!") (No idea what this comment means) :o)
        _audio.PlayPvs(status.Comp.GunShootFailSound, args.Args.Gun);
        _audio.PlayPvs(status.Comp.ClumsySound, args.AppliedTo);
    }

    // Clumsy people sometimes inject themselves!
    [SubscribeLocalEvent]
    private void OnBeforeInjectEvent(Entity<ClumsyInjectorStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeInjectEvent> args)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(args.AppliedTo)))
            return;

        var ev = args.Args;
        ev.TargetGettingInjected = ev.EntityUsingInjector;

        if (status.Comp.FailedMessage != null)
            ev.OverrideMessage = Loc.GetString(status.Comp.FailedMessage);

        _audio.PlayPredicted(status.Comp.ClumsySound, args.AppliedTo, args.AppliedTo);
    }

    // Clumsy people have a blood feud with tables!
    [SubscribeLocalEvent]
    private void OnBeforeClimbEvent(Entity<ClumsyVaultStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeClimbEvent> args)
    {
        if (args.Args.Cancelled
            || !_cfg.GetCVar(CCVars.GameTableBonk)
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(args.AppliedTo)))
            return;

        args.Args.Cancel();

        _climb.Bonk(args.Args.BeingClimbedOn.Owner, args.Args.GettingPutOnTable);

        var putOnTable = Identity.Entity(args.Args.GettingPutOnTable, EntityManager);
        var puttingOnTable = Identity.Entity(args.Args.PuttingOnTable, EntityManager);

        if (args.Args.PuttingOnTable == args.Args.GettingPutOnTable)
        {
            // You are slamming yourself onto the table.

            var selfMessage = status.Comp.SelfFailedMessage == null
                ? null
                : Loc.GetString(status.Comp.SelfFailedMessage, ("bonkable", args.Args.BeingClimbedOn));
            var othersMessage = status.Comp.OtherFailedMessage == null
                ? null
                :  Loc.GetString(status.Comp.OtherFailedMessage, ("victim", putOnTable), ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(selfMessage, othersMessage, args.AppliedTo, args.AppliedTo);
        }
        else
        {
            // Someone else slammed you onto the table.
            // This is only run in server so you need to use popup entity.

            var message = status.Comp.ForcedMessage == null
                ? null
                : Loc.GetString(status.Comp.ForcedMessage,
                    ("bonker", puttingOnTable),
                    ("victim", putOnTable),
                    ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(message, args.AppliedTo);
        }

        _audio.PlayPredicted(status.Comp.ClumsySound, args.Args.GettingPutOnTable, args.Args.GettingPutOnTable);
    }

    #endregion
}
