using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Server.Lightning.Components;

/// <summary>
/// This component allows the lightning system to select a given entity as the target of a lightning strike.
/// It also determines the priority of selecting this target, and the behavior of the explosion. Used for tesla.
/// </summary>
[RegisterComponent]
public sealed partial class LightningTargetComponent : Component
{
    /// <summary>
    /// Priority level for selecting a lightning target. 
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Priority;

    // BOOM PART

    /// <summary>
    /// Will the entity explode after being struck by lightning?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool LightningExplode = true;

    /// <summary>
    /// The explosion prototype to spawn
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ExplosionPrototype> ExplosionPrototype = "Default";

    /// <summary>
    /// The total amount of intensity an explosion can achieve
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TotalIntensity = 25f;

    /// <summary>
    /// How quickly does the explosion's power slope? Higher = smaller area and more concentrated damage, lower = larger area and more spread out damage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Dropoff = 2f;

    /// <summary>
    /// How much intensity can be applied per tile?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxTileIntensity = 5f;
}
