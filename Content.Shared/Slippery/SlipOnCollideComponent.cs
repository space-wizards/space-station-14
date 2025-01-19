using Robust.Shared.GameStates;

namespace Content.Shared.Slippery;

/// <summary>
///     Put on entities to make them slip others when they walk into them (Separate from the step trigger). Also works on throwing ents like Hamlet, where regular <see cref="SlipperyComponent"/> won't
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlipOnCollideComponent : Component
{

}
