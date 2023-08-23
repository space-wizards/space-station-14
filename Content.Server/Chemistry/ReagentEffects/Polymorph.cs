using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class Polymorph : ReagentEffect
{
    /// <summary>
    ///     What polymorph prototype is used on effect
    /// </summary>
    [DataField("prototype")] public string PolymorphPrototype { get; set; }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-make-polymorph",
            ("chance", Probability), ("protoname", prototype.Index<PolymorphPrototype>(PolymorphPrototype).Name));

                    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;
        var polySystem = entityManager.EntitySysManager.GetEntitySystem<PolymorphSystem>();

        // Check if the thing is actually sentient so we dont turn actual plants into trees
        if (!entityManager.HasComponent<MindContainerComponent>(uid))
        {
            return;
        }

        // Make it into a prototype
        entityManager.EnsureComponent<PolymorphableComponent>(uid);
        polySystem.PolymorphEntity(uid, PolymorphPrototype);
    }
}
