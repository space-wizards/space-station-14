using Robust.Shared.GameStates;

namespace Content.Shared.Materials;
/// <summary>
/// Empty component that marks an entity as a "raw" material.
/// The material amounts themselves are in <see cref="PhysicalCompositionComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class MaterialComponent : Component
{

}

