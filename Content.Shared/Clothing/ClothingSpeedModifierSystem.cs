using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    // Public API

    public void SetClothingSpeedModifierEnabled(EntityUid uid, bool enabled, ClothingSpeedModifierComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Enabled != enabled)
        {
            component.Enabled = enabled;
            Dirty(component);

            // inventory system will automatically hook into the event raised by this and update accordingly
            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
            }
        }
    }

    // Event handlers

    private void OnGetState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedModifierComponentState(component.WalkModifier, component.SprintModifier, component.Enabled);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingSpeedModifierComponentState state)
        {
            component.WalkModifier = state.WalkModifier;
            component.SprintModifier = state.SprintModifier;
            component.Enabled = state.Enabled;

            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
            }
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ClothingSpeedModifierComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.Enabled)
            return;

        args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }
}
