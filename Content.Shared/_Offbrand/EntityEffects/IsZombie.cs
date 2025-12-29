using Content.Shared.EntityConditions;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class IsZombieCondition : EntityConditionBase<IsZombieCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-is-zombie", ("invert", Inverted));
    }
}

public sealed class IsZombieConditionEntitySystem : EntityConditionSystem<MetaDataComponent, IsZombieCondition>
{
    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<IsZombieCondition> args)
    {
        args.Result = HasComp<ZombieComponent>(ent);
    }
}
