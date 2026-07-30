using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Store;

/// <summary>
/// Used to define a complicated condition that requires C#
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class ListingCondition
{
    /// <summary>
    /// Determines whether or not a certain entity can purchase a listing.
    /// </summary>
    /// <returns>Whether or not the listing can be purchased</returns>
    public abstract bool Condition(ref ListingConditionArgs args);
}

/// <param name="Buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
/// <param name="Listing">The listing itself</param>
/// <param name="EntityManager">An entitymanager for sane coding</param>
/// <param name="Message">If this string isn't null, </param>
public record struct ListingConditionArgs(EntityUid Buyer, EntityUid? StoreEntity, ListingData Listing, IEntityManager EntityManager, string? Message) { }

