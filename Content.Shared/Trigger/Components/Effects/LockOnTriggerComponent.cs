using Content.Shared.Lock;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will lock, unlock or toggle an entity with the <see cref="LockComponent"/>.
/// If TargetUser is true then they will be (un)locked instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LockOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// If the trigger will lock, unlock or toggle the lock.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LockAction LockMode = LockAction.Toggle;
}

[Serializable, NetSerializable]
public enum LockAction
{
    Lock = 0,
    Unlock = 1,
    Toggle = 2,
}
