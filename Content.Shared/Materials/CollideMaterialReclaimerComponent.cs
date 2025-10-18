namespace Content.Shared.Materials;

/// <summary>
/// Valid items that collide with an entity with this component
/// will begin to be reclaimed.
/// <seealso cref="MaterialReclaimerComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class CollideMaterialReclaimerComponent : Component
{
    /// <summary>
    /// The fixture that starts reclaiming on collision.
    /// </summary>
    [DataField("fixtureId")]
    public string FixtureId = "brrt";
}
