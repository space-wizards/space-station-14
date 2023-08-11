namespace Content.Shared.Mobs.Components;

/// <summary>
/// This is used for...
/// </summary>
public abstract class MobStateActionsComponent : Component
{
    
}

/// <summary>
/// Contains network state for MobStateActionsComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class MobStateActionsComponentState : ComponentState
{
    public MobStateActionsComponentState(MobStateActionsComponent component)
    {

    }
}
