using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger.Components.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LockOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public LockAction LockOnTrigger = LockAction.Toggle;
}

[Serializable, NetSerializable]
public enum LockAction
{
    Lock   = 0,
    Unlock = 1,
    Toggle = 2,
}
