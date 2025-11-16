using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// An entity with this component will not transfer its mind to its brain, such as when the entity is gibbed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MindUntransferableToBrainComponent : Component
{

}
