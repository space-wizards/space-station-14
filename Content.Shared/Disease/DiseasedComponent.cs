using Robust.Shared.GameStates;

namespace Content.Shared.Disease.Components
{
    [NetworkedComponent]
    [RegisterComponent]
    /// This is only used for updating, learn from my mistake don't try having data on update components
    public sealed class DiseasedComponent : Component
    {}
}
