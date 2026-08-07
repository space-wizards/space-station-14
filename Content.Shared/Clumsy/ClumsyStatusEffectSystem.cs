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
        var ent = args.AppliedTo;

        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(ent)))
            return;

        // fail to catch
        var ev = args.Args;
        ev.Cancelled = true;

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(ent, status.Comp.FailDamage, origin: args.Args.Item);

        // todo double check this
        // Collisions don't work properly with PlayPredicted.
        // So we make this server only.
        if (_net.IsClient)
            return;

        _audio.PlayPvs(status.Comp.ClumsySound, ent);

        // todo clean
        var identity = Identity.Entity(ent, EntityManager);
        var selfMessage = status.Comp.SelfFailedMessage == null
            ? null
            : Loc.GetString(status.Comp.SelfFailedMessage, ("item", status.Owner), ("catcher", identity));
        var othersMessage = status.Comp.OtherFailedMessage == null
            ? null
            :  Loc.GetString(status.Comp.OtherFailedMessage, ("item", status.Owner), ("catcher", identity));

        _popup.PopupEntity(selfMessage, othersMessage, ent, ent);
    }

    // Clumsy people shock themselves with defibrillators!
    [SubscribeLocalEvent]
    private void OnBeforeDefibrillatorZapsEvent(Entity<ClumsyDefibStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeDefibrillatorZapsEvent> args)
    {
        var ent = args.AppliedTo;

        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(ent)))
            return;

        var ev = args.Args;
        ev.DefibTarget = ev.EntityUsingDefib;

        _audio.PlayPvs(status.Comp.ClumsySound, ent);

        //todo loc
    }

    // Clumsy people can't be trusted with guns!
    [SubscribeLocalEvent]
    private void OnBeforeGunShotEvent(Entity<ClumsyGunStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeGunShotEvent> args)
    {
        var ent = args.AppliedTo;

        if (args.Args.Gun.Comp.ClumsyProof
            || !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(ent)))
            return;

        if (status.Comp.FailDamage != null)
            _damageable.ChangeDamage(ent, status.Comp.FailDamage, origin: args.Args.Gun);

        _stun.TryUpdateParalyzeDuration(ent, status.Comp.StunDuration);

        // Apply salt to the wound ("Honk!") (No idea what this comment means) (I do :o))
        _audio.PlayPvs(status.Comp.GunShootFailSound, ent);
        _audio.PlayPvs(status.Comp.ClumsySound, ent);

        // todo
        if (status.Comp.SelfFailedMessage != null)
            _popup.PopupEntity(Loc.GetString(status.Comp.SelfFailedMessage), ent, ent);
        args.Args.Cancel();
    }

    // Clumsy people sometimes inject themselves!
    [SubscribeLocalEvent]
    private void OnBeforeInjectEvent(Entity<ClumsyInjectorStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeInjectEvent> args)
    {
        var ent = args.AppliedTo;

        if (!SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(ent)))
            return;

        var ev = args.Args;
        ev.TargetGettingInjected = ev.EntityUsingInjector;

        if (status.Comp.FailedMessage != null)
            ev.OverrideMessage = Loc.GetString(status.Comp.FailedMessage);

        _audio.PlayPredicted(status.Comp.ClumsySound, ent, ent);
    }

    // Clumsy people have a blood feud with tables!
    [SubscribeLocalEvent]
    private void OnBeforeClimbEvent(Entity<ClumsyVaultStatusEffectComponent> status, ref StatusEffectRelayedEvent<SelfBeforeClimbEvent> args)
    {
        var ent = args.AppliedTo;

        if (!_cfg.GetCVar(CCVars.GameTableBonk)
            && !SharedRandomExtensions.PredictedProb(_timing, status.Comp.ClumsyChance, GetNetEntity(status), GetNetEntity(ent)))
            return;

        _climb.Bonk(args.Args.BeingClimbedOn.Owner, args.Args.GettingPutOnTable);

        _audio.PlayPredicted(status.Comp.ClumsySound, status, status);

        var gettingPutOnTableName = Identity.Entity(args.Args.GettingPutOnTable, EntityManager);
        var puttingOnTableName = Identity.Entity(args.Args.PuttingOnTable, EntityManager);

        if (args.Args.PuttingOnTable == ent)
        {
            // You are slamming yourself onto the table.

            var selfMessage = status.Comp.SelfFailedMessage == null
                ? null
                : Loc.GetString(status.Comp.SelfFailedMessage, ("bonkable", args.Args.BeingClimbedOn));
            var othersMessage = status.Comp.OtherFailedMessage == null
                ? null
                :  Loc.GetString(status.Comp.OtherFailedMessage, ("victim", gettingPutOnTableName), ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(selfMessage, othersMessage, ent, ent);
        }
        else
        {
            // Someone else slammed you onto the table.
            // This is only run in server so you need to use popup entity.

            var message = status.Comp.ForcedMessage == null
                ? null
                : Loc.GetString(status.Comp.ForcedMessage,
                    ("bonker", puttingOnTableName),
                    ("victim", gettingPutOnTableName),
                    ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(message, ent);
        }

        //args.Args.Cancel();
    }

    #endregion
}
