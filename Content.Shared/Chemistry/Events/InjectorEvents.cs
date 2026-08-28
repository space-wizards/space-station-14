using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Events;

/// <summary>
/// Raised on the injector when the doafter has finished.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class InjectorDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// The base injection attempt event. It'll be raised on the user and target when attempting to inject the target.
/// </summary>
/// <param name="user">The user who is trying to inject the target.</param>
/// <param name="usedInjector">The injector being used by the user.</param>
/// <param name="target">The target who the user is trying to inject.</param>
/// <param name="overrideMessage">The resulting message that gets displayed per popup.</param>
public abstract partial class BeforeInjectTargetEvent(EntityUid user, EntityUid usedInjector, EntityUid target, string? overrideMessage = null)
    : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public EntityUid EntityUsingInjector = user;
    public readonly EntityUid UsedInjector = usedInjector;
    public EntityUid TargetGettingInjected = target;
    public string? OverrideMessage = overrideMessage;
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
///     This event is raised on the user using the injector before the injector is injected.
///     The event is triggered on the user and all their clothing.
/// </summary>
public sealed class SelfBeforeInjectEvent(EntityUid user, EntityUid usedInjector, EntityUid target, string? overrideMessage = null)
    : BeforeInjectTargetEvent(user, usedInjector, target, overrideMessage);

/// <summary>
///     This event is raised on the target before the injector is injected.
///     The event is triggered on the target itself and all its clothing.
/// </summary>
[ByRefEvent]
public sealed class TargetBeforeInjectEvent(EntityUid user, EntityUid usedInjector, EntityUid target, string? overrideMessage = null)
    : BeforeInjectTargetEvent(user, usedInjector, target, overrideMessage);
