using Content.Shared.Physics;

namespace Content.Shared._Starlight.Fixtures;

/// <summary>
/// This is used for mixing soft and hard fixtures.
/// TODO: If the engine PR for this gets merged, delete this file and all related files and use the engine component
/// </summary>
[RegisterComponent]
public sealed partial class MixedFixturesComponent : Component
{
    [DataField] public int Mask = (int)CollisionGroup.LowImpassable;
}