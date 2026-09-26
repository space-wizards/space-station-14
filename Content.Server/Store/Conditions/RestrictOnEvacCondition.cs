using Content.Server.Shuttles.Systems;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

public sealed partial class RestrictOnEvacCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        return !args.EntityManager.System<EmergencyShuttleSystem>().EmergencyShuttleArrived;
    }
}
