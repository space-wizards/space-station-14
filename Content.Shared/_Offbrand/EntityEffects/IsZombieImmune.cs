using Content.Shared.EntityConditions;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class IsZombieImmuneCondition : EntityConditionBase<IsZombieImmuneCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-is-zombie-immune", ("invert", Inverted));
    }
}

public sealed class IsZombieImmuneConditionEntitySystem : EntityConditionSystem<MetaDataComponent, IsZombieImmuneCondition>
{
    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<IsZombieImmuneCondition> args)
    {
        args.Result = HasComp<ZombieImmuneComponent>(ent);
    }
}
