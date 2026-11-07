using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

/// <summary>
/// A plant mutation prototype reference, its odds, and optional overrides.
/// Resolve the effective settings when using the mutation so copied entries do not need initialization.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomPlantMutation
{
    /// <summary>
    /// The prototype supplying defaults for fields not overridden by this entry.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RandomPlantMutationPrototype> Prototype;

    [DataField]
    public float BaseOdds;

    /// <summary>
    /// Overrides the prototype's default value for Name.
    /// </summary>
    [DataField("name")]
    private string? _nameOverride;

    /// <summary>
    /// Overrides the prototype's default value for Description.
    /// </summary>
    [DataField("description")]
    private LocId? _descriptionOverride;

    /// <summary>
    /// Overrides the prototype's default value for Effect.
    /// </summary>
    [DataField("effect")]
    private EntityEffect? _effectOverride;

    /// <summary>
    /// Overrides the prototype's default value for AppliesToPlant.
    /// </summary>
    [DataField("appliesToPlant")]
    private bool? _appliesToPlantOverride;

    /// <summary>
    /// Overrides the prototype's default value for AppliesToProduce.
    /// </summary>
    [DataField("appliesToProduce")]
    private bool? _appliesToProduceOverride;

    /// <summary>
    /// Overrides the prototype's default value for Persists.
    /// </summary>
    [DataField("persists")]
    private bool? _persistsOverride;

    /// <summary>
    /// Grabs the prototype's values, overriding them if overrides are defined.
    /// </summary>
    public ResolvedPlantMutation Resolve(IPrototypeManager prototypes)
    {
        var prototype = prototypes.Index(Prototype);

        return new ResolvedPlantMutation(
            _nameOverride ?? prototype.Name,
            _descriptionOverride ?? prototype.Description,
            _effectOverride ?? prototype.Effect,
            _appliesToProduceOverride ?? prototype.AppliesToProduce,
            _appliesToPlantOverride ?? prototype.AppliesToPlant,
            _persistsOverride ?? prototype.Persists);
    }
}

/// <summary>
/// The plant mutation after overrides are applied
/// </summary>
public readonly record struct ResolvedPlantMutation(
    string Name,
    LocId? Description,
    EntityEffect Effect,
    bool AppliesToProduce,
    bool AppliesToPlant,
    bool Persists);
