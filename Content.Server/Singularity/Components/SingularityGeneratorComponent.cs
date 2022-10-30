using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
public sealed class SingularityGeneratorComponent : Component
{
    /// <summary>
    /// The amount of power this generator has accumulated.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetPower"/>
    /// </summary>
    [DataField("power")]
    [Access(friends:typeof(SingularityGeneratorSystem), Self=AccessPermissions.Read, Other=AccessPermissions.Read)]
    public float Power = 0;

    /// <summary>
    /// The power threshold at which this generator will spawn a singularity.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetThreshold"/>
    /// </summary>
    [DataField("threshold")]
    [Access(friends:typeof(SingularityGeneratorSystem), Self=AccessPermissions.Read, Other=AccessPermissions.Read)]
    public float Threshold = 16;

    /// <summary>
    ///     The prototype ID used to spawn a singularity.
    /// </summary>
    [DataField("spawnId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SpawnId = "Singularity";
}
