using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Components;

// must be networked to properly predict adding & removal
[NetworkedComponent, RegisterComponent]
public sealed class SlowedByContactComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Intersecting = new();
}
