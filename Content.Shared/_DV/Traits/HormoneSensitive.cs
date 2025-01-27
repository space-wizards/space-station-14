namespace Content.Shared._DV.Traits;
using Content.Shared.Humanoid;

/// <summary>
/// This is used for the hormone sensitivty traits.
/// </summary>
[RegisterComponent]
public sealed partial class HormoneSensitiveComponent : Component
{
    [DataField(required: true)]
    public Sex Target;
}
