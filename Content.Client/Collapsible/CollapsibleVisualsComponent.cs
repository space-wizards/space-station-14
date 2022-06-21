using Content.Shared.Hands.Components;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Client.Collapsible;

/// <summary>
/// Holds the visuals for collapsible items;
/// </summary>
[RegisterComponent]
public sealed class CollapsibleVisualsComponent : Component
{
    [DataField("collapsedState", required: true)]
    public string CollapsedState = default!;

    [DataField("extendedState", required: true)]
    public string ExtendedState = default!;
}
