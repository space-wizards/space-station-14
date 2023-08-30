using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
public sealed partial class SingularityGeneratorComponent : Component
{
    /// <summary>
    /// The amount of power this generator has accumulated.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetPower"/>
    /// </summary>
    [DataField("power")]
    [Access(friends:typeof(SingularityGeneratorSystem))]
    public float Power = 0;

    /// <summary>
    /// The power threshold at which this generator will spawn a singularity.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetThreshold"/>
    /// </summary>
    [DataField("threshold")]
    [Access(friends:typeof(SingularityGeneratorSystem))]
    public float Threshold = 16;

    /// <summary>
    ///     The prototype ID used to spawn a singularity.
    /// </summary>
    [DataField("spawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? SpawnPrototype = "Singularity";
}
