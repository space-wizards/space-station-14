using Content.Shared.EntityEffects;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class IsZombie : EntityEffectCondition
{
    [DataField]
    public bool Invert = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<ZombieComponent>(args.TargetEntity) ^ Invert;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-is-zombie", ("invert", Invert));
    }
}
