using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Species;

/// <summary>
///     Allows an entity to use thirst for a speed boost.
///     Also allows that speed boost to produce a fluid.
/// </summary>
public sealed partial class SharedSnailSprintSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private SatiationSystem _satiationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailSprintComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SnailSprintComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<SnailSprintComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SnailSprintComponent, SnailSprintActionEvent>(OnSnailSprintAction);
        SubscribeLocalEvent<SnailSprintComponent, SnailSprintDoAfterEvent>(OnSnailSprintDoAfter);
    }

    /// <summary>
    ///     Gives the action to the entity
    /// </summary>
    private void OnComponentStartup(Entity<SnailSprintComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    ///     Removes the action from the entity.
    /// </summary>
    private void OnCompRemove(Entity<SnailSprintComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    /// <summary>
    ///     Run when the SnailSprintAction is used.
    /// </summary>
    private void OnSnailSprintAction(Entity<SnailSprintComponent> ent, ref SnailSprintActionEvent args)
    {
        // prevent the action and let the player know if their thirst value is too low to use it.
        if (TryComp<SatiationComponent>(ent.Owner, out var satiationComponent)
            //&& thirstComp.CurrentThirstThreshold < ent.Comp.MinThirstThreshold)
            && _satiationSystem.IsValueInRange((ent.Owner, satiationComponent),
                SatiationSystem.Thirst,
                ent.Comp.MinThirstThreshold))
        {
            _popupSystem.PopupEntity(Loc.GetString(ent.Comp.FailedPopup), ent.Owner, ent.Owner);
            return;
        }
        // if not...

        // let the games begin.
        ent.Comp.Active = true;
        // refresh movementSpeedModifiers. this will run OnRefreshMovespeed.
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        // create the doafter and declare its arguments
        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.SprintLength, new SnailSprintDoAfterEvent(), ent.Owner)
        {
            // this is inherited from Sericulture Component.
            // This should definitely be in YML, but i don't know how to do that
            BreakOnMove = false,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        // run the doafter that will end the effects after comp.SprintLength time has passed.
        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    /// <summary>
    ///     Runs every frame.
    ///     In this case, ensures that mucin is only spilled on new tiles, and not repeatedly spilled on the same tile.
    /// </summary>
    public override void Update(float frameTime)
    {
        // create an EntityQueryEnumerator, which loops through every entity with a given component
        var enumerator = EntityQueryEnumerator<SnailSprintComponent>();

        // while the entity being queried is this one, do our tile logic
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                continue; // skip the entity if the action is not active

            var xform = Transform(uid);
            if (xform.GridUid == null)
                continue; // skip the entity if the entity is not on a grid

            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
                continue; // another way to skip the entity if the entity is not on a grid

            // if the entity is on a grid, and the action is active,
            // if the server is the one running this, and there is a reagent defined,
            if (!_netManager.IsClient && comp.ReagentProduced != null) // verifying that the server is running this is necessary because this is in Content.Shared
            {
                // get the current tile that the entity is on,
                var tile = _mapSystem.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

                // if it's not invalid and different from the last tile,
                if (tile != TileRef.Zero && tile != comp.LastTile)
                {
                    // create a new Solution object
                    var solution = new Solution();
                    // add the specified amount of the specified reagent to it
                    solution.AddReagent(comp.ReagentProduced!, comp.ReagentQuantity);
                    // and spill it all over da floor.
                    _puddleSystem.TrySpillAt(Transform(uid).Coordinates, solution, out _);
                }

                // finally, set the last tile to the current tile.
                comp.LastTile = tile;
            }
        }
    }

    /// <summary>
    ///     Runs after the doafter finishes. Removes the speed modifier and stops the production of mucin.
    ///     It's important that this runs regardless of whether or not the action is cancelled. We don't want infinitely-speedy snails.
    /// </summary>
    private void OnSnailSprintDoAfter(Entity<SnailSprintComponent> ent, ref SnailSprintDoAfterEvent args)
    {
        // Ensure that it only runs once.
        args.Repeat = false;

        // Ensure that the next time RefreshMovementSpeedModifiersEvent is raised, OnRefreshMovespeed will remove the movement speed modifier
        ent.Comp.Active = false;
        // then raise that event
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        // remove the thirst cost from total thirst
        if (!TryComp<SatiationComponent>(ent.Owner, out var satiationComponent))
            return;

        _satiationSystem.ModifyValue((ent.Owner, satiationComponent),
            SatiationSystem.Thirst,
            -ent.Comp.ThirstCost);
    }

    /// <summary>
    ///     Run when the RefreshMovementSpeedModifiersEvent is raised.
    /// </summary>
    private void OnRefreshMovespeed(Entity<SnailSprintComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // if the action has been used and the doafter is not over, apply speed boost.
        // if the action has not been used, or the doafter has finished, reset speed.
        args.ModifySpeed(ent.Comp.Active ? ent.Comp.SpeedBoost : 1f);
    }
}

/// <summary>
///     Relayed upon using the action.
/// </summary>
public sealed partial class SnailSprintActionEvent : InstantActionEvent;

/// <summary>
///     Is relayed after the doafter finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SnailSprintDoAfterEvent : SimpleDoAfterEvent;
