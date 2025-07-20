// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class InfectiodDeadMutation : EntityEffect
{
    [DataField]
    public float MutationStrength;

    [DataField]
    public bool IsStableMutation;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-mutate-infection-dead", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<NecromorfSystem>().MutateVirus(args.TargetEntity, MutationStrength, IsStableMutation);
    }
}

