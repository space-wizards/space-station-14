namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// Marks all entities that should become invisible or more translucent
/// for bilnd characters.
/// </summary>
[RegisterComponent]
public sealed partial class InvisibleToBlindComponent : Component
{
    /// <summary>
    /// Should this entity be visible when character closes their eyes
    /// </summary>
    [DataField]
    public bool Visible = false;

    /// <summary>
    /// Was this entity visible before making it invisible?
    /// </summary>
    /// <remarks>
    /// Required because of issue 38838
    /// </remarks>
    [DataField]
    public bool OldVisible = true;

    /// <summary>
    /// New alpha channel of entity sprite if its supposed to be visible, but less.
    /// </summary>
    /// <remarks>
    /// Ignored when <see cref="Visible"/> is set to false.
    /// Values from 0 to 1 inclusive
    /// </remarks>
    [DataField]
    public float Alpha = 0.2f;

    /// <summary>
    /// Alpha channel before making entity less visible.
    /// </summary>
    /// <remarks>
    /// Required because of issue 38838
    /// </remarks>
    [DataField]
    public float OldAlpha = Color.Black.A;
}
