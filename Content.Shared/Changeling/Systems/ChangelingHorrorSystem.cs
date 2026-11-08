using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
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
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

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
    /// This function will only be executed when transforming from changeling horror to a "regular" person.
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

        // VERY CURSED CODE AREA: MOVING THE HORROR'S INVENTORY INTO LINGSPACE
        // i have become death, destroyer of decent code
        // we first drop items that are in slots that depend upon other slots
        var itemsDeps = _inventory.GetHandOrInventoryEntities(ent.Owner).Where((k) =>
        {
            var s = _inventory.TryGetContainingSlot(k, out var slot);
            return s;
        }).Select((item) =>
        {
            var s = _inventory.TryGetContainingSlot(item, out var slotNullable);
            var slot = slotNullable ?? throw new Exception("we literally checked if it was null 6 lines before");

            // drop only if it depends on something
            var success = slot.DependsOn != null ? _inventory.TryUnequip(ent.Owner, slot.Name, silent: true, force: true) : false;
            return (item, slot.Name, success);

        }).Where((tuple) =>
        {
            // todo: check if the entity was deleted, which can probably be the case for things such as hardsuit helmet etc
            return tuple.success;
        }).Select(
                (tuple) => (tuple.item, tuple.Name)
            ).ToList();

        var items = _inventory.GetHandOrInventoryEntities(ent.Owner).Where((k) =>
        {
            var s = _inventory.TryGetContainingSlot(k, out var slot);
            return s;
        }).Select((item) =>
        {
            var s = _inventory.TryGetContainingSlot(item, out var slotNullable);
            var slot = slotNullable ?? throw new Exception("we literally checked if it was null 6 lines before");
            // drop: in theory it should have no depends on but lets make sure, just in case...
            var success = slot.DependsOn == null ? _inventory.TryUnequip(ent.Owner, slot.Name, silent: true, force: true) : false;
            return (item, slot.Name, success);

        }).Where((tuple) =>
        {
            // todo: check if the entity was deleted, which can probably be the case for things such as hardsuit helmet etc
            return tuple.success;
        }).Select(
                (tuple) => (tuple.item, tuple.Name)
            ).ToList();

        if (items != null)
        {
            // if they are after in the list their dependency should be present before them upon reequiping
            if (itemsDeps != null)
                items.AddRange(itemsDeps ?? throw new Exception("shouldnt be null"));

            if (!_identitySystem.PausedMapId.HasValue)
                return;

            var mapCoords = new MapCoordinates(0, 0, _identitySystem.PausedMapId.Value);

            // throw in nullspace
            foreach (var item in items)
            {
                _transform.SetMapCoordinates(item.item, mapCoords);
            }

            AddComp(ent.Owner, new ChangelingHorrorItemsComponent()
            {
                LingSpaceItems = items
            });
        }

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
    /// This function should only be executed when the changeling transforms back into normal urist mchandness. It gives the ling back their items
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterTransformInventory(Entity<ChangelingHorrorItemsComponent> ent, ref AfterChangelingTransformEvent ev)
    {
        if (!HasComp<ChangelingHorrorComponent>(ev.PreviousIdentity))
            return;

        // some more cursed code for the inventory
        foreach (var item in ent.Comp.LingSpaceItems)
        {
            _inventory.TryEquip(ent.Owner, item.item, item.slot, silent : true, force : true);
        }
        ent.Comp.LingSpaceItems.Clear();
        RemCompDeferred<ChangelingHorrorItemsComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemsShutdown(Entity<ChangelingHorrorItemsComponent> ent, ref ComponentRemove args)
    {
        // pull the items back from lingspace so that valuable things arent lost if the horror form is ashed, for instance
        var coords = _transform.GetMapCoordinates(ent.Owner);
        foreach (var item in ent.Comp.LingSpaceItems)
        {
            _transform.SetMapCoordinates(item.item, coords);
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

    [SubscribeLocalEvent]
    public void OnTryDevourEvent(Entity<ChangelingHorrorComponent> ent, ref ChangelingTryDevourEvent args)
    {
        args.BreakOnDamage = false;
        args.Multiplier *= ent.Comp.DevourSpeedMultiplier;
    }

    [SubscribeLocalEvent]
    public void OnTryWindupEvent(Entity<ChangelingHorrorComponent> ent, ref ChangelingDevourTryWindupEvent args)
    {
        // its mouth is already bare
        args.Multiplier = 0f;
    }

    [SubscribeLocalEvent]
    public void OnEntityDevoured(Entity<ChangelingHorrorComponent> ent, ref ChangelingEntityDevouredEvent args)
    {
        // convert new dna to time
        if (TryComp<StoreComponent>(ent.Owner, out var store))
        {
            if (store.Balance.ContainsKey(_currency))
            {
                var k = store.Balance[_currency];
                // remove all DNA points from the store, since they are being converted into time
                Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> dico = new() {
                    {_currency, -k }
                };
                // note that we dont add a grace period here (set to 0d) because this is mid-transformation
                _stores.TryAddCurrency(dico, ent.Owner, store);
                ent.Comp.EndTime += DNAToTime(k, ent.Comp.SecondPerDNA.TotalSeconds, 0d);
                Dirty(ent);
            }
        }
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
