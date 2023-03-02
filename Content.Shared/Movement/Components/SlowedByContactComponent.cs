using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

// must be networked to properly predict adding & removal
[NetworkedComponent, RegisterComponent]
public sealed class SlowedByContactComponent : Component
{
    [ViewVariables]
    public bool Refresh = false;

    [ViewVariables]
    public readonly HashSet<EntityUid> Intersecting = new();

    [DataField("fixture")]
    public string Fixture = "fixture_1";
}
