using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adds or removes a plant trait.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeTraitsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeTraits>
{
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeTraits> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        var traitType = _componentFactory.GetComponent(args.Effect.Trait);
        if (traitType is not PlantTraitsComponent)
        {
            Log.Error(
                $"Component '{traitType}' (type: {traitType.GetType().Name}) is not a descendant of {nameof(PlantTraitsComponent)}.");
            return;
        }

        switch (args.Effect.Type)
        {
            case PlantChangeTraits.TraitModifyType.Add:
                AddComp(entity.Owner, traitType);
                break;
            case PlantChangeTraits.TraitModifyType.Remove:
                RemCompDeferred(entity.Owner, traitType.GetType());
                break;
            case PlantChangeTraits.TraitModifyType.Toggle:
                if (HasComp(entity.Owner, traitType.GetType()))
                    RemCompDeferred(entity.Owner, traitType.GetType());
                else
                    AddComp(entity.Owner, traitType);
                break;
            default:
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeTraits : EntityEffectBase<PlantChangeTraits>
{
    /// <summary>
    /// Name of a <see cref="PlantTraitsComponent"/> type.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Trait;

    /// <summary>
    /// Defines how the trait should be modified.
    /// </summary>
    [DataField]
    public TraitModifyType Type = TraitModifyType.Toggle;

    public enum TraitModifyType
    {
        /// <summary>
        /// Adds the trait if it is not already present.
        /// </summary>
        Add,

        /// <summary>
        /// Removes the trait if it is present.
        /// </summary>
        Remove,

        /// <summary>
        /// Adds the trait if it is not present, or removes it if it is already present.
        /// </summary>
        Toggle
    }

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var component = IoCManager.Resolve<IComponentFactory>().GetComponent(Trait);
        if (component is not PlantTraitsComponent plantTrait || plantTrait.TraitName is not { } traitName)
        {
            return null;
        }

        return Loc.GetString("entity-effect-guidebook-plant-change-trait", [("change", Type.ToString()), ("chance", Probability), ("trait", Loc.GetString(traitName))]);
    }
}
