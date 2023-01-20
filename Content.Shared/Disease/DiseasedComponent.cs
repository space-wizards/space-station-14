using Robust.Shared.GameStates;

namespace Content.Shared.Disease.Components
{
    /// This is added to anyone with at least 1 disease
    /// and helps cull event subscriptions and entity queries
    /// when they are not relevant.
    [NetworkedComponent]
    [RegisterComponent]
    public sealed class DiseasedComponent : Component
    {}
}
