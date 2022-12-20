namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for adjusting something's height.
/// </summary>
[RegisterComponent]
public sealed class HeightAdjustedComponent : Component
{
    [DataField("height", required: true)]
    public float Height { get; }
}
