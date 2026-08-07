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
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _net = default!;

    #region Subscriptions

    // Clumsy people are bad at baseball!
    [SubscribeLocalEvent]
    private void OnCatchAttempt(Entity<ClumsyCatchStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CatchAttemptEvent> args)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ClumsyChance, GetNetEntity(ent), GetNetEntity(args.Args.Item)))
            return;

        // fail to catch
        var ev = args.Args;
        ev.Cancelled = true;

        if (ent.Comp.FailDamage != null)
            _damageable.ChangeDamage(ent.Owner, ent.Comp.FailDamage, origin: args.Args.Item);

        // todo double check this
        // Collisions don't work properly with PlayPredicted.
        // So we make this server only.
        if (_net.IsClient)
            return;

        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        // todo clean
        var identity = Identity.Entity(ent.Owner, EntityManager);
        var selfMessage = ent.Comp.SelfFailedMessage == null
            ? null
            : Loc.GetString(ent.Comp.SelfFailedMessage, ("item", ent.Owner), ("catcher", identity));
        var othersMessage = ent.Comp.OtherFailedMessage == null
            ? null
            :  Loc.GetString(ent.Comp.OtherFailedMessage, ("item", ent.Owner), ("catcher", identity));

        _popup.PopupEntity(selfMessage, othersMessage, ent.Owner, ent.Owner);
    }

    // Clumsy people shock themselves with defibrillators!
    [SubscribeLocalEvent]
    private void OnDefibrillatorZapsEvent(Entity<ClumsyDefibStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeDefibrillatorZapsEvent> args)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ClumsyChance, GetNetEntity(ent), GetNetEntity(args.Args.Defib)))
            return;

        var ev = args.Args;
        ev.DefibTarget = ev.EntityUsingDefib;

        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        //todo loc
    }

    // Clumsy people can't be trusted with guns!
    [SubscribeLocalEvent]
    private void OnGunShotEvent(Entity<ClumsyGunStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeGunShotEvent> args)
    {
        if (args.Args.Gun.Comp.ClumsyProof
            || !SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ClumsyChance, GetNetEntity(ent), GetNetEntity(args.Args.Gun)))
            return;

        if (ent.Comp.FailDamage != null)
            _damageable.ChangeDamage(ent.Owner, ent.Comp.FailDamage, origin: args.Args.Gun);

        _stun.TryUpdateParalyzeDuration(ent, ent.Comp.StunDuration);

        // Apply salt to the wound ("Honk!") (No idea what this comment means) (I do :o))
        _audio.PlayPvs(ent.Comp.GunShootFailSound, ent);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        // todo
        if (ent.Comp.SelfFailedMessage != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SelfFailedMessage), ent, ent);
        args.Args.Cancel();
    }

    // Clumsy people sometimes inject themselves!
    [SubscribeLocalEvent]
    private void BeforeHyposprayEvent(Entity<ClumsyHypoStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeInjectEvent> args)
    {
        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ClumsyChance, GetNetEntity(ent), GetNetEntity(args.Args.UsedInjector)))
            return;

        var ev = args.Args;
        ev.TargetGettingInjected = ev.EntityUsingInjector;

        if (ent.Comp.FailedMessage != null)
            ev.OverrideMessage = Loc.GetString(ent.Comp.FailedMessage);

        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, args.Args.EntityUsingInjector);
    }

    // Clumsy people have a blood feud with tables!
    [SubscribeLocalEvent]
    private void OnBeforeClimbEvent(Entity<ClumsyVaultStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeClimbEvent> args)
    {
        if (!_cfg.GetCVar(CCVars.GameTableBonk)
            && !SharedRandomExtensions.PredictedProb(_timing, ent.Comp.ClumsyChance, GetNetEntity(ent), GetNetEntity(args.Args.BeingClimbedOn)))
            return;

        _climb.Bonk(args.Args.BeingClimbedOn.Owner, args.Args.GettingPutOnTable);

        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, ent);

        var gettingPutOnTableName = Identity.Entity(args.Args.GettingPutOnTable, EntityManager);
        var puttingOnTableName = Identity.Entity(args.Args.PuttingOnTable, EntityManager);

        if (args.Args.PuttingOnTable == ent.Owner)
        {
            // You are slamming yourself onto the table.

            var selfMessage = ent.Comp.SelfFailedMessage == null
                ? null
                : Loc.GetString(ent.Comp.SelfFailedMessage, ("bonkable", args.Args.BeingClimbedOn));
            var othersMessage = ent.Comp.OtherFailedMessage == null
                ? null
                :  Loc.GetString(ent.Comp.OtherFailedMessage, ("victim", gettingPutOnTableName), ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(selfMessage, othersMessage, ent, ent);
        }
        else
        {
            // Someone else slamed you onto the table.
            // This is only run in server so you need to use popup entity.

            var message = ent.Comp.ForcedMessage == null
                ? null
                : Loc.GetString(ent.Comp.ForcedMessage,
                    ("bonker", puttingOnTableName),
                    ("victim", gettingPutOnTableName),
                    ("bonkable", args.Args.BeingClimbedOn));

            _popup.PopupEntity(message, ent);
        }

        args.Args.Cancel();
    }

    #endregion
}
