using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Polymorph : EntityEffectBase<Polymorph>
{
    /// <summary>
    /// Polymorph configuration to apply.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype;

    /// <summary>
    /// Allows polymorphing entities without a PolymorphableComponent.
    /// Cooldowns and restrictions on repeated polymorphs still apply.
    /// </summary>
    [DataField]
    public bool Force;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-polymorph",
            ("chance", Probability),
            ("entityname", prototype.Index<EntityPrototype>(prototype.Index(Prototype).Configuration.Entity).Name));
}
