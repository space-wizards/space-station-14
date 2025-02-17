using Content.Shared.Inventory;
using JetBrains.Annotations;

namespace Content.Shared.DeadSpace.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentStartup(EntityUid uid, NightVisionComponent comp, ComponentStartup args)
    {
        ToggleNightVision(uid, comp);
    }

    private void OnComponentRemove(EntityUid uid, NightVisionComponent comp, ComponentRemove args)
    {
        ToggleNightVision(uid, comp);
    }

    private void ToggleNightVision(EntityUid uid, NightVisionComponent comp)
    {
        comp.IsNightVision = !comp.IsNightVision;
        var changeEv = new NightVisionChangedEvent(comp.IsNightVision);
        RaiseLocalEvent(uid, ref changeEv);
        Dirty(uid, comp);
    }

    [PublicAPI]
    public void UpdateIsNightVision(Entity<NightVisionComponent?> nvComponent)
    {
        if (!Resolve(nvComponent, ref nvComponent.Comp, false))
            return;

        var old = nvComponent.Comp.IsNightVision;

        var ev = new CanNightVisionAttemptEvent();
        RaiseLocalEvent(nvComponent.Owner, ev);
        nvComponent.Comp.IsNightVision = ev.NightVision;

        if (old == nvComponent.Comp.IsNightVision)
            return;

        var changeEv = new NightVisionChangedEvent(nvComponent.Comp.IsNightVision);
        RaiseLocalEvent(nvComponent.Owner, ref changeEv);
        Dirty(nvComponent);
    }
}

/// <summary>
///     This event is raised when an entity's night vision status changes
/// </summary>
[ByRefEvent]
public record struct NightVisionChangedEvent(bool Blind);

/// <summary>
///     Raised directed at an entity to see whether the entity is currently have night vision or not.
/// </summary>
public sealed class CanNightVisionAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public bool NightVision => Cancelled;
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}
