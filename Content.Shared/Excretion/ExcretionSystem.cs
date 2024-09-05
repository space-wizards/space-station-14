using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.Nutrition.Components;
using Content.Shared.Stacks;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Excretion;

/// <summary>
/// Allows mobs to produce materials using Thirst with <see cref="ExcretionComponent"/>.
/// </summary>
public abstract partial class SharedExcretionSystem : EntitySystem
{
    // Managers
    [Dependency] private readonly INetManager _netManager = default!;
	[Dependency] private readonly IPrototypeManager _proto = default!;

    // Systems
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
	[Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
	[Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
	[Dependency] private readonly SharedJetpackSystem _jetpack = default!;
	[Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
	[Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExcretionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ExcretionComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<ExcretionComponent, ExcretionActionEvent>(OnExcretionStart);
        SubscribeLocalEvent<ExcretionComponent, ExcretionDoAfterEvent>(OnExcretionDoAfter);
    }

    /// <summary>
    /// Giveths the action to preform excretion on the entity
    /// </summary>
    private void OnMapInit(EntityUid uid, ExcretionComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action);
		/// refreshes the movement speed modifier so that the snailSlowdownModifier triggers.
		_movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Takeths away the action to preform excretion from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, ExcretionComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }
	// When the action is started, grab thirst information.
    private void OnExcretionStart(EntityUid uid, ExcretionComponent comp, ExcretionActionEvent args)
    {
        if (TryComp<ThirstComponent>(uid, out var thirstComp)
        && _thirstSystem.IsThirstBelowState(uid, comp.MinThirstThreshold, thirstComp.CurrentThirst - comp.ThirstCost, thirstComp))
        {
            _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, comp.ProductionLength, new ExcretionDoAfterEvent(), uid)
        { // I'm not sure if more things should be put here, but imo ideally it should probably be set in the component/YAML. Not sure if this is currently possible.
            BreakOnMove = false,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }


    private void OnExcretionDoAfter(EntityUid uid, ExcretionComponent comp, ExcretionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

		if(TryComp<ThirstComponent>(uid, out var thirstComp))
		{
			if(_thirstSystem.IsThirstBelowState(uid, comp.MinThirstThreshold, thirstComp.CurrentThirst - comp.ThirstCost, thirstComp))
			{
				_popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
				return;
			}
		_thirstSystem.ModifyThirst(uid, thirstComp, -comp.ThirstCost);
		}

        if (!_netManager.IsClient) // Have to do this because spawning stuff in shared is CBT.
        {
			/*
			/// if there aren't values for the desired reagent and its volume, don't do anything.
			if ((comp.ExcretedReagent != "") &&
			(comp.ExcretedVolume !<= 0))
			{
			*/
			/// declare a solution and add the specified amount of the specified reagent to it.
			var solution = new Solution();
			solution.AddReagent(comp.ExcretedReagent, comp.ExcretedVolume);
			/// then, spill that reagent @ the entity's coordinates
			_puddleSystem.TrySpillAt(Transform(uid).Coordinates, solution, out _);
			///}
        }

        args.Repeat = false;
    }

}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class ExcretionActionEvent : InstantActionEvent { }

/// <summary>
/// Is relayed at the end of the sericulturing doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ExcretionDoAfterEvent : SimpleDoAfterEvent { }
