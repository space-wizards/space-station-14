using Robust.Shared.Serialization;

namespace Content.Shared.Tutorial;

/// <summary>
/// This is used for...
/// </summary>
public abstract partial class TutorialComponent : Component
{
    
}

/// <summary>
/// Contains network state for TutorialComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class TutorialComponentState : ComponentState
{
    public TutorialComponentState(TutorialComponent component)
    {

    }
}
