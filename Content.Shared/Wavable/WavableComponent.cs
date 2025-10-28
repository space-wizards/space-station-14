namespace Content.Shared.Wavable;

/// <summary>
///     A component added to entities that can be waved.
/// </summary>
[RegisterComponent]
public sealed partial class WavableComponent : Component
{
    [DataField]
    public bool UseInHand = true;
}
