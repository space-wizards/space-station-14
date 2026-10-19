using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Systems;

// The horror form works through weird hacks around the fact that we're trying to strip an humanoid of what makes it an humanoid without actually making it not an humanoid.
// TODO: make this work in a better way. It shouldn't need to be a whole specie with only a torso sprite and invisible organs.

/// <summary>
/// Handles transforming to / from the horror form, including the timed limit & the handing out of actions.
/// </summary>
public abstract partial class ChangelingHorrorSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedChangelingIdentitySystem _identitySystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedStoreSystem _stores = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    // constants
    /// <summary>
    /// Currency that is used to maintain the horror form
    /// </summary>
    private readonly ProtoId<CurrencyPrototype> _currency = "ChangelingDNA";
    /// <summary>
    /// The horror form's prototype
    /// </summary>
    private readonly EntProtoId _protoId = "MobHorror";
    #region transformation

    [SubscribeLocalEvent]
    private void OnChangelingTransformIntoEvent(Entity<ChangelingHorrorComponent> ent, ref ChangelingAttemptTransformIntoEvent args)
    {
        // Stores are not properly networked.
        if (_net.IsClient)
        {
            args.Cancelled = true;
            return;
        }

        // if we are trying to transform into horror form, check for DNA
        if (TryComp<StoreComponent>(args.Changeling, out var store))
        {
            if (store.Balance.ContainsKey(_currency))
            {
                var k = store.Balance[_currency];
                if (k >= FixedPoint2.New(ent.Comp.MinimumDna)) // you need at least one dna point
                    return;
            }
        }

        args.Cancelled = true;
        args.Reason = Loc.GetString("changeling-horror-transform-fail");

    }

    /// <summary>
    /// This function will only be executed when transforming to changeling horror to a "regular" person.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforeTransform(Entity<ChangelingHorrorComponent> ent, ref BeforeChangelingTransformEvent args)
    {
        // this event fires before the transformation (but after the doafter)
        if (HasComp<ChangelingHorrorComponent>(args.StoredIdentity))
            return; // we shouldn't be transforming into a horror!

        // enable actions again
        foreach (var action in _actions.GetActions(ent.Owner))
        {
            if (HasComp<ChangelingHorrorDisableComponent>(action.Owner))
            {
                _actions.SetEnabled((action.Owner, action.Comp), true);
            }
        }

        // Remove the alert that displays time
        _alerts.ClearAlert(ent.Owner, ent.Comp.TimeAlert);

        // Add dna points back
        if (TryComp<StoreComponent>(ent.Owner, out var storeComp))
        {
            // do fancy math to add back DNA based on remaining time
            Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> dico = new() {
                {_currency, TimeToDNA(ent.Comp.EndTime - Timing.CurTime, ent.Comp.SecondPerDNA.TotalSeconds, ent.Comp.GracePeriod.TotalSeconds) }
                };
            _stores.TryAddCurrency(dico, ent.Owner, storeComp);
        }

        // removed actions
        foreach (var action in ent.Comp.StoredActions)
        {
            _actions.RemoveAction(action);
        }

        ent.Comp.StoredActions.Clear();
    }

    /// <summary>
    /// Fired when the horror mode is unlocked.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnlock(Entity<ChangelingIdentityComponent> ent, ref ChangelingUnlockHorrorEvent ev)
    {
        var idEnt = Spawn(_protoId); // todo: make this into a generic system that unlocks identities (can be used for the lesser form etc.)
        var identity = _identitySystem.GrantIdentity((ent.Owner, ent.Comp), idEnt);
        if (identity.HasValue)
        {
            EnsureComp<ChangelingUncountedIdentityComponent>(identity.Value);
            EnsureComp<ChangelingUnremovableIdentityComponent>(identity.Value);
        }

        QueueDel(idEnt); // we dont need to keep this entity any longer
    }

    [SubscribeLocalEvent]
    private void OnBeforeTransform(Entity<ChangelingTransformComponent> ent, ref BeforeChangelingTransformEvent args)
    {
        if (!HasComp<ChangelingHorrorComponent>(args.StoredIdentity))
            return;

        // Turn actions on/off
        foreach (var action in _actions.GetActions(ent.Owner))
        {
            if (TryComp<ChangelingHorrorDisableComponent>(action.Owner, out var comp))
            {
                if (comp.ToggleOff)
                {
                    if (action.Comp.Toggled) // we perform the action. lets really hope this toggles it off, okay?
                        _actions.PerformAction((ent.Owner, null), (action.Owner, action.Comp));

                    // we force it, just in case
                    _actions.SetToggled((action.Owner, action.Comp), false);
                }

                _actions.SetEnabled((action.Owner, action.Comp), false);
            }
        }
    }

    /// <summary>
    /// This fonction should only be executed when the changeling transforms into its horror form
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterTransform(Entity<ChangelingHorrorComponent> ent, ref AfterChangelingTransformEvent ev)
    {
        // fires after the transformation
        // transformed into a changeling horror, spawn VFX station-wide, toggle actions, etc
        if (!HasComp<ChangelingHorrorComponent>(ev.StoredIdentity))
            return; // this shouldn't happen...

        // calculate timing
        var transformationTime = ent.Comp.GracePeriod; // you get some free seconds!

        if (TryComp<StoreComponent>(ent.Owner, out var store))
        {
            if (store.Balance.ContainsKey(_currency))
            {
                var k = store.Balance[_currency];
                // remove all DNA points from the store, since they are being converted into time
                Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> dico = new() {
                    {_currency, -k }
                };
                _stores.TryAddCurrency(dico, ent.Owner, store);
                transformationTime = DNAToTime(k, ent.Comp.SecondPerDNA.TotalSeconds, ent.Comp.GracePeriod.TotalSeconds);
            }
        }

        ent.Comp.EndTime = Timing.CurTime + transformationTime;
        ent.Comp.LastIdentity = ev.PreviousIdentity;

        Dirty(ent);

        // this alert will display the time
        _alerts.ShowAlert(ent.Owner, ent.Comp.TimeAlert);

        // apply effects, this should include healing
        if (ent.Comp.SpawnEffects != null)
            _effects.ApplyEffects(ent.Owner, ent.Comp.SpawnEffects);

        // Uncuff
        if (TryComp<CuffableComponent>(ent.Owner, out _) && _cuffable.TryGetLastCuff(ent.Owner, out var cuff))
            _cuffable.Uncuff(ent.Owner, ent.Owner, cuff.Value);

        // play a spawn sound
        _audio.PlayPredicted(ent.Comp.SpawnSound, ent.Owner, null);

        PredictedSpawnAttachedTo(ent.Comp.SpawnScreech, new EntityCoordinates(ent.Owner, Vector2.Zero));

        if (ent.Comp.Actions == null)
            return;

        foreach (var actionProto in ent.Comp.Actions)
        {
            EntityUid? actEnt = null;
            _actions.AddAction(ent.Owner, ref actEnt, actionProto);
            if (actEnt.HasValue)
                ent.Comp.StoredActions.Add(actEnt.Value);
        }
    }

    [SubscribeLocalEvent]
    public void OnIsSafeEvent(Entity<ChangelingHorrorComponent> ent, ref IsIdentitySafeEvent args)
    {
        args.IsSafe = false;
    }
    #endregion
    #region helpers
    /// <summary>
    /// Converts an amount of DNA currency into horror mode time.
    /// </summary>
    public static TimeSpan DNAToTime(FixedPoint2 dna, double secondPerDNA, double grace)
    {
        return TimeSpan.FromSeconds((double)dna * secondPerDNA + grace);
    }

    /// <summary>
    /// Returns the horror mode time to its DNA worth. Note that going inbetween conversions is lossy.
    /// </summary>
    public static FixedPoint2 TimeToDNA(TimeSpan time, double secondPerDNA, double grace)
    {
        var seconds = time.TotalSeconds - grace;
        var dna = Math.Max(0, (int)(seconds / secondPerDNA));
        return FixedPoint2.New(dna);
    }
    #endregion
}

/// <summary>
/// Unlocks an entity's horror form
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ChangelingUnlockHorrorEvent : EntityEventArgs;
