using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

/// <summary>
/// Used to define a complicated condition that requires C#
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract class ListingCondition
{
    /// <summary>
    /// Determines whether or not a certain entity can purchase a listing.
    /// </summary>
    /// <returns>Whether or not the listing can be purchased</returns>
    public abstract bool Condition(ListingConditionArgs args);
}

/// <param name="User">The person purchasing the listing</param>
/// <param name="listing">The liting itself</param>
/// <param name="EntityManager">An entitymanager for sane coding</param>
public readonly record struct ListingConditionArgs(EntityUid User, ListingData listing, IEntityManager EntityManager);
