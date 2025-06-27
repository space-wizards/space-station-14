using Content.Shared._Impstation.Ghost;
using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Impstation.EntityEffects.Effects;

public sealed partial class Medium : EntityEffect
{
    /// <summary>
    ///     What Medium prototype is used on effect
    /// </summary>
    //[DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<MediumPrototype>))]
    //public string MediumPrototype { get; set; }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Grants whoever drinks this the ability to see ghosts for a while";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;

        entityManager.EnsureComponent<MediumComponent>(uid);
    }
}
