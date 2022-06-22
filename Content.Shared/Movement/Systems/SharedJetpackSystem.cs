using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, ToggleActionEvent>(OnJetpackToggle);
        SubscribeLocalEvent<JetpackComponent, GotEquippedEvent>(OnJetpackEquipped);
        SubscribeLocalEvent<JetpackComponent, GotEquippedHandEvent>(OnJetpackHandEquipped);
        SubscribeLocalEvent<JetpackComponent, GotUnequippedEvent>(OnJetpackUnequipped);
        SubscribeLocalEvent<JetpackComponent, GotUnequippedHandEvent>(OnJetpackHandUnequipped);

        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, MobMovementProfileEvent>(OnJetpackUserMovement);
    }

    private void OnJetpackUserMovement(EntityUid uid, JetpackUserComponent component, ref MobMovementProfileEvent args)
    {
        // Only overwrite jetpack movement if they're offgrid.
        if (args.Override || !args.Weightless) return;

        args.Override = true;
        args.Acceleration = component.Acceleration;
        args.WeightlessModifier = component.WeightlessModifier;
        args.Friction = component.Friction;
    }

    private void OnJetpackUserCanWeightless(EntityUid uid, JetpackUserComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnJetpackHandUnequipped(EntityUid uid, JetpackComponent component, GotUnequippedHandEvent args)
    {
        if (!component.Enabled) return;
        RemComp<JetpackUserComponent>(uid);
    }

    private void OnJetpackUnequipped(EntityUid uid, JetpackComponent component, GotUnequippedEvent args)
    {
        if (!component.Enabled) return;
        RemComp<JetpackUserComponent>(uid);
    }

    private void OnJetpackHandEquipped(EntityUid uid, JetpackComponent component, GotEquippedHandEvent args)
    {
        if (!component.Enabled) return;
        EnsureComp<JetpackUserComponent>(args.User);
    }

    private void OnJetpackEquipped(EntityUid uid, JetpackComponent component, GotEquippedEvent args)
    {
        if (!component.Enabled) return;
        EnsureComp<JetpackUserComponent>(args.Equipee);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleActionEvent args)
    {
        if (args.Handled) return;

        SetEnabled(component, !component.Enabled);
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }

    public void SetEnabled(JetpackComponent component, bool enabled)
    {
        if (component.Enabled == enabled ||
            enabled && !CanEnable(component)) return;

        component.Enabled = enabled;
        if (enabled)
        {
            EnsureComp<ActiveJetpackComponent>(component.Owner);
        }
        else
        {
            RemComp<ActiveJetpackComponent>(component.Owner);
        }

        if (Container.TryGetContainingContainer(component.Owner, out var container) &&
            HasComp<InventoryComponent>(container.Owner))
        {
            if (enabled)
            {
                EnsureComp<JetpackUserComponent>(container.Owner);
            }
            else
            {
                RemComp<JetpackUserComponent>(container.Owner);
            }
        }

        TryComp<AppearanceComponent>(component.Owner, out var appearance);
        appearance?.SetData(JetpackVisuals.Enabled, enabled);
        Dirty(component);
    }

    protected abstract bool CanEnable(JetpackComponent component);

    [Serializable, NetSerializable]
    protected sealed class JetpackComponentState : ComponentState
    {
        public bool Enabled;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
