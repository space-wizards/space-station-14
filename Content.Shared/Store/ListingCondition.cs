using JetBrains.Annotations;

namespace Content.Shared.Store;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract class ListingCondition
{
    public abstract bool Condition(ListingConditionArgs args);
}

public readonly record struct ListingConditionArgs(EntityUid User, ListingData listing, IEntityManager EntityManager);
