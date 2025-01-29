using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Combat.Ranged.Pierce;
[RegisterComponent]
public sealed partial class PierceableComponent : Component
{
    [DataField]
    public PierceLevel Level = PierceLevel.Metal;
}
[Serializable, NetSerializable]
public enum PierceLevel : byte
{
    Flesh,
    Wood,
    Metal,
    HardenedMetal,
    Rock,
}
