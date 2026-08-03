using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Screech;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Systems;

/// <summary>
/// Handles transforming to / from the horror form, including the timed limit & the handing out of actions.
/// </summary>
public abstract partial class SharedChangelingHorrorSystem : EntitySystem
{
    [Dependency] private SharedChangelingIdentitySystem _identitySystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStoreSystem _stores = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private ChangelingTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stuns = default!;
    [Dependency] private SharedPopupSystem _popups = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ScreechSystem _screech = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingUnlockHorrorEvent>(OnUnlock);
        SubscribeLocalEvent<ChangelingHorrorComponent, AfterChangelingTransformEvent>(OnAfterTransform);
        SubscribeLocalEvent<ChangelingHorrorComponent, BeforeChangelingTransformEvent>(OnBeforeTransform);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<ChangelingHorrorComponent, ChangelingIdentityComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var identities))
        {
            // todo: check for paused maps etc.

            // display the time alert
            _alerts.ShowAlert(uid, comp.TimeAlert);

            // calculate the timeout
            if (_timing.CurTime - comp.InitialTime > comp.TimeBudget)
            {
                // we try to find a non-horror identity
                var id = identities.ConsumedIdentities.Where(k => !HasComp<ChangelingHorrorComponent>(k.Identity));

                if (!id.Any())
                    continue;

                var identity = id.First();

                if (!identity.Identity.HasValue)
                    return;

                // we force the transformation, this will call all cleanup code in OnBeforeTransform
                var tComp = EnsureComp<ChangelingTransformComponent>(uid);
                _transform.TransformIntoNow((uid, tComp), identity.Identity.Value);

                var selfMessage = Loc.GetString("changeling-horror-force-transform-self", ("user", Identity.Entity(uid, EntityManager)));
                var othersMessage = Loc.GetString("changeling-horror-force-transform-others", ("user", Identity.Entity(uid, EntityManager)));
                _popups.PopupPredicted(
                selfMessage,
                othersMessage,
                uid,
                uid,
                PopupType.MediumCaution);

                // we apply a stun penality, you should transform back yourself!
                _stuns.TryAddStunDuration(uid, TimeSpan.FromSeconds(10));
                _stuns.TryKnockdown(uid, TimeSpan.FromSeconds(10));
            }
        }
    }

    #region transformation
    /// <summary>
    /// This function will only be executed when transforming to changeling horror to a "regular" person.
    /// </summary>
    private void OnBeforeTransform(Entity<ChangelingHorrorComponent> ent, ref BeforeChangelingTransformEvent args)
    {
        // this event fires before the transformation (but after the doafter)
        if (HasComp<ChangelingHorrorComponent>(args.StoredIdentity))
            return; // we shouldn't be transforming into an horror!

        // enable actions again
        foreach (var action in _actions.GetActions(ent.Owner))
        {
            if (TryComp<ChangelingHorrorDisableComponent>(action.Owner, out var comp))
            {
                if (comp.ToggleOff)
                {
                    _actions.SetToggled((action.Owner, action.Comp), comp.OldToggleStatus);
                }

                _actions.SetEnabled((action.Owner, action.Comp), true);
            }
        }

        // remove horror actions
        if (TryComp<ChangelingHorrorActionStorageComponent>(ent.Owner, out var lingActions))
        {
            foreach (var action in lingActions.CreatedActions)
            {
                _actions.RemoveAction(ent.Owner, action);
            }

            lingActions.CreatedActions.Clear();
        }

        // Remove the alert that displays time
        _alerts.ClearAlert(ent.Owner, ent.Comp.TimeAlert);

        // Add dna points back
        if (TryComp<StoreComponent>(ent.Owner, out var _))
        {
            // do fancy math to add back DNA based on remaining time
            Dictionary<string, FixedPoint2> dico = new() {
                {"ChangelingDNA", TimeToDNA(ent.Comp.TimeBudget - (_timing.CurTime - ent.Comp.InitialTime)) }
                };
            _stores.TryAddCurrency(dico, ent.Owner);
        }
    }

    /// <summary>
    /// Fired when the horror mode is unlocked.
    /// </summary>
    private void OnUnlock(Entity<ChangelingIdentityComponent> ent, ref ChangelingUnlockHorrorEvent ev)
    {
        var idEnt = Spawn("MobHorror"); // todo: make this into a generic system that unlocks identities (can be used for the lesser form etc.)
        var identity = _identitySystem.GrantIdentity((ent.Owner, ent.Comp), idEnt);
        if (identity.HasValue)
        {
            AddComp(identity.Value, new ChangelingUncountedIdentityComponent());
            AddComp(identity.Value, new ChangelingUnremovableIdentityComponent());
        }

        QueueDel(idEnt); // we dont need to keep this entity any longer
    }

    /// <summary>
    /// This fonction should only be executed when the changeling transforms into its horror form
    /// </summary>
    protected virtual void OnAfterTransform(Entity<ChangelingHorrorComponent> ent, ref AfterChangelingTransformEvent ev)
    {
        // fires after the transformation
        // transformed into a changeling horror, spawn VFX station-wide, toggle actions, etc
        if (!HasComp<ChangelingHorrorComponent>(ev.StoredIdentity))
            return; // this shouldn't happen...

        // calculate timing
        var now = _timing.CurTime;
        var transformationTime = TimeSpan.FromSeconds(5);// you get 5 free seconds!

        if (TryComp<StoreComponent>(ent.Owner, out var store))
        {
            if (store.Balance.ContainsKey("ChangelingDNA"))
            {
                var k = store.Balance["ChangelingDNA"];
                // remove all DNA points from the store, since they are being converted into time
                Dictionary<string, FixedPoint2> dico = new() {
                    {"ChangelingDNA", -k }
                };
                _stores.TryAddCurrency(dico, ent.Owner);
                transformationTime = DNAToTime(k);
            }
        }

        ent.Comp.TimeBudget = transformationTime;
        ent.Comp.InitialTime = now;

        // this alert will display the time
        _alerts.ShowAlert(ent.Owner, ent.Comp.TimeAlert);

        // full heal
        RaiseLocalEvent(ent.Owner, new RejuvenateEvent());

        // Uncuff
        if (TryComp<CuffableComponent>(ent.Owner, out _) && _cuffable.TryGetLastCuff(ent.Owner, out var cuff))
            _cuffable.Uncuff(ent.Owner, ent.Owner, cuff.Value);

        // spawn an evil-ass screech VFX
        // TODO: handle stunning etc. here
        var screechEnt = _screech.Screech(ent.Owner, ent.Comp.SpawnScreechRange, ent.Comp.SpawnScreechVfx);

        if (screechEnt.HasValue)
            MakeGlobal(screechEnt.Value);

        // play a spawn sound
        _audio.PlayPredicted(ent.Comp.SpawnSound, ent.Owner, null);

        // Turn actions on/off
        foreach (var action in _actions.GetActions(ent.Owner))
        {
            if (TryComp<ChangelingHorrorDisableComponent>(action.Owner, out var comp))
            {
                if (comp.ToggleOff)
                {
                    comp.OldToggleStatus = action.Comp.Toggled;
                    _actions.SetToggled((action.Owner, action.Comp), false);
                }

                _actions.SetEnabled((action.Owner, action.Comp), false);
            }
        }

        // give horror actions
        if (TryComp<ChangelingHorrorActionStorageComponent>(ent.Owner, out var lingActions))
        {
            // this shouldn't be needed, but just in case...
            lingActions.CreatedActions.Clear();

            foreach (var action in lingActions.Actions)
            {
                var k = _actions.AddAction(ent.Owner, action);
                if (k.HasValue)
                {
                    // we keep track of them to delete them later when turning back
                    lingActions.CreatedActions.Add(k.Value);
                }
            }
        }
    }
    #endregion
    #region helpers
    protected abstract void MakeGlobal(EntityUid ent);
    /// <summary>
    /// Converts an amount of DNA currency into horror mode time
    /// </summary>
    public static TimeSpan DNAToTime(FixedPoint2 dna)
    {
        return TimeSpan.FromSeconds((double)dna * 3d + 5d);
    }

    /// <summary>
    /// Returns the horror mode time to its DNA worth. Note that going inbetween conversions is lossy.
    /// </summary>
    public static FixedPoint2 TimeToDNA(TimeSpan time)
    {
        var seconds = time.TotalSeconds - 5;
        var dna = Math.Max(0, (int)(seconds / 3d));
        return FixedPoint2.New(dna);
    }
    #endregion
}

/// <summary>
/// Unlocks an entity's horror form
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingUnlockHorrorEvent : EntityEventArgs;
