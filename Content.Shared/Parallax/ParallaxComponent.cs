using Robust.Shared.GameStates;

namespace Content.Shared.Parallax;

/// <summary>
/// Handles per-map parallax
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ParallaxComponent : Component
{
    // I wish I could use a typeserializer here but parallax is extremely client-dependent.
    [ViewVariables(VVAccess.ReadWrite), DataField("parallax")]
    public string Parallax = "Default";
}
