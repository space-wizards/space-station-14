namespace Content.Shared.Strip.Components;

/// <summary>
/// Give this to an entity when you want to increase their stripping times
/// </summary>
[RegisterComponent]
public sealed class ThievingComponent : Component
{
    /// <summary>
    /// How much the strip time should be shortened by
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stealTime")]
    public float StealTime = 0.5f;

    /// <summary>
    /// Should it notify the user if they're stripping a pocket?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stealthy")]
    public bool Stealthy;
}
