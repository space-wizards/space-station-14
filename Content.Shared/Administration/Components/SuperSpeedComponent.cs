namespace Content.Shared.Administration.Components;

public abstract partial class SharedSuperSpeedComponent : Component
{
    /// <summary>
    /// Not sure why you'd want to modify this, but here you go.
    /// </summary>
    [DataField]
    public void MovementMultiplier = 400f;
}
