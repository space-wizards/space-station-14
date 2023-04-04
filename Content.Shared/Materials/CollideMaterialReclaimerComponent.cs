namespace Content.Shared.Materials;

/// <summary>
/// This is used for a <see cref="MaterialReclaimerComponent"/>
/// </summary>
[RegisterComponent]
public sealed class CollideMaterialReclaimerComponent : Component
{
    /// <summary>
    /// The fixture that starts reclaiming on collision.
    /// </summary>
    [DataField("fixtureId")]
    public string FixtureId = "brrt";
}
