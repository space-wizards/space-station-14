using Robust.Shared.GameObjects;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Marker component that causes <c>DefectSystem</c> to roll each
/// <see cref="DefectComponent.Prob"/> at MapInit, removing defects that fail.
/// </summary>
[RegisterComponent]
public sealed partial class RandomDefectsComponent : Component
{
}
