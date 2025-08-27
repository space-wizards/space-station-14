// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class NecromorphMutagen : EntityEffect
{
    [DataField]
    public bool IsAnimal = false;

    [DataField("necroPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string? NecroPrototype { get; set; } = null;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-necromorph-mutagen", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;

        if (entityManager.HasComponent<HumanoidAppearanceComponent>(target) && IsAnimal)
            return;

        var component = args.EntityManager.EnsureComponent<NecromorfAfterInfectionComponent>(target);
        component.NecroPrototype = NecroPrototype;
    }
}
