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

    /// <summary> Clumsy people are bad at baseball! </summary>
    [SubscribeLocalEvent]
    private void OnCatchAttemptEvent(Entity<ClumsyCatchStatusEffectComponent> status, ref CatchAttemptEvent args)
    {
        var user = args.User;
        if (args.Cancelled
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(user)))
            return;

        args.Cancelled = true;

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(user, status.Comp.FailDamage, origin: args.Item);

        var identity = Identity.Entity(user, EntityManager);

        var selfMessage = status.Comp.SelfFailedMessage == null
            ? null
            : Loc.GetString(status.Comp.SelfFailedMessage, ("item", args.Item));
        var othersMessage = status.Comp.OtherFailedMessage == null
            ? null
            : Loc.GetString(status.Comp.OtherFailedMessage, ("item", args.Item), ("catcher", identity));

        _popup.PopupEntity(selfMessage, othersMessage, user, user);

        // _audio.PlayPredicted doesn't play nice with collision events so we need PlayPvs
        // exit early for clients so the sound doesn't play twice
        if (_net.IsClient)
            return;

        _audio.PlayPvs(status.Comp.ClumsySound, user);
    }

    /// <summary> Clumsy people shock themselves with defibrillators! </summary>
    [SubscribeLocalEvent]
    private void OnBeforeDefibrillatorZapsEvent(Entity<ClumsyDefibStatusEffectComponent> status, ref SelfBeforeDefibrillatorZapsEvent args)
    {
        var user = args.EntityUsingDefib;
        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(user)))
            return;

        args.DefibTarget = args.EntityUsingDefib;

        if (status.Comp.FailedMessage != null)
            _popup.PopupEntity(Loc.GetString(status.Comp.FailedMessage), user, user);

        _audio.PlayPredicted(status.Comp.ClumsySound, user, user);
    }

    /// <summary> Clumsy people can't be trusted with guns! </summary>
    [SubscribeLocalEvent]
    private void OnBeforeGunShotEvent(Entity<ClumsyGunStatusEffectComponent> status, ref SelfBeforeGunShotEvent args)
    {
        var user = args.Shooter;
        if (args.Cancelled
            || args.Gun.Comp.ClumsyProof
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(user)))
            return;

        args.Cancel();

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(user, status.Comp.FailDamage, origin: args.Gun);

        _stun.TryUpdateParalyzeDuration(user, status.Comp.StunDuration);

        if (status.Comp.FailedMessage != null)
            _popup.PopupEntity(Loc.GetString(status.Comp.FailedMessage, ("gun", args.Gun)), user, user);

        // SelfBeforeGunShotEvent is raised on server so _audio.PlayPredicted fails to play locally
        if (_net.IsClient)
            return;

        // Apply salt to the wound ("Honk!") (No idea what this comment means) :o)
        _audio.PlayPvs(status.Comp.GunShootFailSound, args.Gun);
        _audio.PlayPvs(status.Comp.ClumsySound, user);
    }

    /// <summary> Clumsy people sometimes inject themselves! </summary>
    [SubscribeLocalEvent]
    private void OnBeforeInjectEvent(Entity<ClumsyInjectorStatusEffectComponent> status, ref SelfBeforeInjectEvent args)
    {
        var user = args.EntityUsingInjector;
        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(user)))
            return;

        var ev = args;
        ev.TargetGettingInjected = ev.EntityUsingInjector;

        if (status.Comp.FailedMessage != null)
            ev.OverrideMessage = Loc.GetString(status.Comp.FailedMessage);

        _audio.PlayPredicted(status.Comp.ClumsySound, user, user);
    }

    /// <summary> Clumsy people have a blood feud with tables! </summary>
    [SubscribeLocalEvent]
    private void OnBeforeClimbEvent(Entity<ClumsyVaultStatusEffectComponent> status, ref SelfBeforeClimbEvent args)
    {
        var target = args.GettingPutOnTable;
        var user = args.PuttingOnTable;
        if (args.Cancelled ||
            !_cfg.GetCVar(CCVars.GameTableBonk) ||
            !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(target)))
            return;

        args.Cancel();

        _climb.Bonk(args.BeingClimbedOn.Owner, target);

        var putOnTable = Identity.Entity(target, EntityManager);
        var puttingOnTable = Identity.Entity(user, EntityManager);

        if (target == user)
        {
            // You are slamming yourself onto the table.

            var selfMessage = status.Comp.SelfFailedMessage == null
                ? null
                : Loc.GetString(status.Comp.SelfFailedMessage, ("bonkable", args.BeingClimbedOn));
            var othersMessage = status.Comp.OtherFailedMessage == null
                ? null
                : Loc.GetString(status.Comp.OtherFailedMessage, ("victim", putOnTable), ("bonkable", args.BeingClimbedOn));

            _popup.PopupEntity(selfMessage, othersMessage, user, user);
        }
        else
        {
            // Someone else slammed you onto the table.

            var message = status.Comp.ForcedMessage == null
                ? null
                : Loc.GetString(status.Comp.ForcedMessage,
                    ("bonker", puttingOnTable),
                    ("victim", putOnTable),
                    ("bonkable", args.BeingClimbedOn));

            _popup.PopupEntity(message, target);
        }

        _audio.PlayPredicted(status.Comp.ClumsySound, target, user);
    }

    #endregion
}
