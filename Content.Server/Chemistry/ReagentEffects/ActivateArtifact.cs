using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class ActivateArtifact : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        var artifact = args.EntityManager.EntitySysManager.GetEntitySystem<ArtifactSystem>();
        artifact.TryActivateArtifact(args.SolutionEntity);
    }
}
