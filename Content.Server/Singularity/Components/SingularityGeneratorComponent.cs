using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
public sealed class SingularityGeneratorComponent : Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    ///     The amount of power this generator has accumulated.
    /// </summary>
    [Access(friends:typeof(SingularityGeneratorSystem))]
    public int _power;

    [DataField("power")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Power
    {
        get => _power;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SingularityGeneratorSystem>().SetPower(this, value); }
    }

    /// <summary>
    ///     The power threshold at which this generator will spawn a singularity.
    /// </summary>
    [DataField("threshold")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Threshold = 16;

    /// <summary>
    ///     The prototype ID used to spawn a singularity.
    /// </summary>
    [DataField("spawnId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SpawnId = "Singularity";
}
