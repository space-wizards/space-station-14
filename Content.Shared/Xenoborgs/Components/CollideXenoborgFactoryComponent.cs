namespace Content.Shared.Xenoborgs.Components;

/// <summary>
/// Valid items that collide with an entity with this component
/// will begin to be reclaimed.
/// <seealso cref="XenoborgFactoryComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class CollideXenoborgFactoryComponent : Component
{
    /// <summary>
    /// The fixture that starts reclaiming on collision.
    /// </summary>
    [DataField]
    public string FixtureId = "brrt";
}
