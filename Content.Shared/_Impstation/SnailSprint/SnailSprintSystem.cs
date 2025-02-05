using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids;
using Content.Shared.Movement.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Content.Shared.Coordinates;
using Linguini.Syntax.Ast;
using Microsoft.Extensions.ObjectPool;
using Content.Shared.GameTicking;

namespace Content.Shared._Impstation.SnailSprint;

/// <summary>
/// Allows an entity to use thirst for a speed boost. Also allows that speed boost to produce a fluid.
/// </summary>
public sealed partial class SharedSnailSprintSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
    /// Gives the action to the entity
    /// </summary>
    private void OnComponentStartup(Entity<SnailSprintComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    /// Removes the action from the entity.
    /// </summary>
    private void OnCompRemove(Entity<SnailSprintComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    /// <summary>
    /// Run when the SnailSprintAction is used.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    private void OnSnailSprintAction(Entity<SnailSprintComponent> ent, ref SnailSprintActionEvent args)
    {
        // prevent the action and let the player know if their thirst value is too low to use it. 
        if (TryComp<ThirstComponent>(ent.Owner, out var thirstComp) && _thirstSystem.IsThirstBelowState(ent, ent.Comp.MinThirstThreshold, thirstComp.CurrentThirst - ent.Comp.ThirstCost, thirstComp))
        {
            _popupSystem.PopupClient(Loc.GetString(ent.Comp.FailedPopup), ent.Owner, ent.Owner);
            return;
        }
        // if not...

        // let the games begin.
        ent.Comp.Active = true;
        // refresh movementSpeedModifiers. this will run OnRefreshMovespeed.
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        // create the doafter and declare its arguments
        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.SprintLength, new SnailSprintDoAfterEvent(), ent.Owner)
        { // this is inherited from Sericulture Component. This should definitely be in YML, but i don't know how to do that
            BreakOnMove = false,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        // run the doafter that will end the effects after comp.SprintLength time has passed.
        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Runs every frame. In this case, ensures that mucin is only spilled on new tiles, and not repeatedly spilled on the same tile.
    /// </summary>
    /// <param name="frameTime"></param>
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
    /// Runs after the doafter finishes. Removes the speed modifier and stops the production of mucin. It's important that this runs regardless of whether or not the action is cancelled. We don't want infinitely-speedy snails.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    private void OnSnailSprintDoAfter(Entity<SnailSprintComponent> ent, ref SnailSprintDoAfterEvent args)
    {
        // Ensure that it only runs once.
        args.Repeat = false;

        // Ensure that the next time RefreshMovementSpeedModifiersEvent is raised, OnRefreshMovespeed will remove the movement speed modifier
        ent.Comp.Active = false;
        // then raise that event
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);

        // remove the thirst cost from total thirst
        if (TryComp<ThirstComponent>(ent.Owner, out var thirstComp))
        {
            _thirstSystem.ModifyThirst(ent.Owner, thirstComp, -ent.Comp.ThirstCost);
        }
    }

    /// <summary>
    /// Run when the RefreshMovementSpeedModifiersEvent is raised.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="comp"></param>
    /// <param name="args"></param>
    private void OnRefreshMovespeed(Entity<SnailSprintComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // if the action has been used and the doafter is not over, apply speed boost.
        if (ent.Comp.Active == true)
        {
            args.ModifySpeed(ent.Comp.SpeedBoost);
        }

        // if the action has not been used, or the doafter has finished, reset speed.
        else
        {
            args.ModifySpeed(1f);
        }
    }
}

/// <summary>
/// Relayed upon using the action.
/// </summary>
public sealed partial class SnailSprintActionEvent : InstantActionEvent { }

/// <summary>
/// Is relayed after the doafter finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SnailSprintDoAfterEvent : SimpleDoAfterEvent { }
