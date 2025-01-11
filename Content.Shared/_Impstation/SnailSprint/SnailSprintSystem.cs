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

namespace Content.Shared._Impstation.SnailSprint;
// TODO:
// - check if you have enough thirst X
// - figure out a way to spill only upon moving to a new tile. spill every x miliseconds otherwise
// - actually modify the speed of the ent
// - wait for the duration specified in the comp X
// - end the effects, then remove the thirst cost

/// <summary>
/// Allows an entity to use thirst for a speed boost. Also allows that speed boost to produce a fluid.
/// </summary>
public abstract partial class SharedSnailSprintSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;

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

        // create a new solution, fill it with the specified amount of reagent, and spill it on the floor.
        if (!_netManager.IsClient && ent.Comp.ReagentProduced != null) // verifying that you are the server is required for spawning entities because the component is in shared.
        {
            var solution = new Solution();
            solution.AddReagent(ent.Comp.ReagentProduced!, ent.Comp.ReagentQuantity);
            _puddleSystem.TrySpillAt(Transform(ent.Owner).Coordinates, solution, out _);
        }

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

        // remove the thirst cost
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
