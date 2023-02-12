namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaComponent : Component
{
    /// Currently worn suit
    [DataField("suit")]
    public EntityUid? Suit = null;
}
