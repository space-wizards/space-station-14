using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Public API for Ingestion System so you can build your own form of ingestion system.
/// </summary>
public sealed partial class IngestionSystem
{
    public const float MaxFeedDistance = 1.0f; // We should really have generic interaction ranges like short, medium, long and use those instead...
    // BodySystem has no way of telling us where the mouth is so we're making some assumptions.
    public const SlotFlags DefaultFlags = SlotFlags.HEAD | SlotFlags.MASK;

    #region Generic

    /// <summary>
    /// Tries to make an entity ingest another entity.
    /// </summary>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    /// <param name="ingest">Whether we're actually ingesting the item or if we're just testing.</param>
    /// <returns>Returns true if we can ingest the item.</returns>
    public bool TryIngest(EntityUid user, EntityUid target, EntityUid ingested, bool ingest = true)
    {
        var eatEv = new IngestibleEvent();
        RaiseLocalEvent(ingested, ref eatEv);

        if (eatEv.Cancelled)
            return false;

        var ingestionEv = new CanIngestEvent(user, ingested, ingest);
        RaiseLocalEvent(target, ref ingestionEv);

        return ingestionEv.Handled;
    }

    /// <summary>
    ///     Check whether we have an open pie-hole that's in range.
    /// </summary>
    /// <param name="user">The one performing the action</param>
    /// <param name="target">The target whose mouth is checked</param>
    /// <param name="popupUid">Optional entity that will receive an informative pop-up identifying the blocking
    /// piece of equipment.</param>
    /// <returns></returns>
    public bool HasMouthAvailable(EntityUid user, EntityUid target, EntityUid? popupUid = null)
    {
        return HasMouthAvailable(user, target, DefaultFlags, popupUid);
    }

    /// <inheritdoc cref="HasMouthAvailable(EntityUid, EntityUid, EntityUid?)"/>
    /// Overflow which takes custom flags for a mouth being blocked, in case the entity has a mouth not on the face.
    public bool HasMouthAvailable(EntityUid user, EntityUid target, SlotFlags flags, EntityUid? popupUid = null)
    {
        if (!_transform.GetMapCoordinates(user).InRange(_transform.GetMapCoordinates(target), MaxFeedDistance))
        {
            var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
            _popup.PopupClient(message, user, user);
            return false;
        }

        var attempt = new IngestionAttemptEvent(flags);
        RaiseLocalEvent(target, ref attempt);

        if (!attempt.Cancelled)
            return true;

        if (attempt.Blocker != null && popupUid != null)
            _popup.PopupClient(Loc.GetString("food-system-remove-mask", ("entity", attempt.Blocker.Value)), user, popupUid.Value);

        return false;
    }

    #endregion

    #region EdibleComponent
    public bool CanIngestEdible(EntityUid user, EntityUid target, Entity<EdibleComponent?> ingested)
    {
        return CanIngestEdible(user, target, ingested, out _);
    }

    /// <summary>
    ///     Checks if we can feed an edible solution to a target and returns the solution.
    /// </summary>
    /// <param name="user">The one doing the feeding</param>
    /// <param name="target">The one being fed.</param>
    /// <param name="ingested">The food item being eaten.</param>
    /// <param name="solution">The solution we will be consuming from.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity</returns>
    public bool CanIngestEdible(EntityUid user,
        EntityUid target,
        Entity<EdibleComponent?> ingested,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;

        if (!Resolve(ingested, ref ingested.Comp))
            return false;

        if (!HasMouthAvailable(user, target))
            return false;

        // If we don't have the tools to eat we can't eat.
        return TryAccessSolution(ingested.Owner, user, out solution);
    }

    /// <summary>
    /// Estimate the number of bites this food has left, based on how much food solution there is and how much of it to eat per bite.
    /// </summary>
    public int GetUsesRemaining(Entity<EdibleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return 0;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution) || solution.Volume == 0)
            return 0;

        // eat all in 1 go, so non empty is 1 bite
        if (entity.Comp.TransferAmount == null)
            return 1;

        return Math.Max(1, (int) Math.Ceiling((solution.Volume / (FixedPoint2) entity.Comp.TransferAmount).Float()));
    }

    #endregion

    #region Solutions

    /// <summary>
    /// Checks if the item is currently edible.
    /// </summary>
    public bool TryAccessSolution(Entity<SolutionContainerManagerComponent?> ingested,
        EntityUid user,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        bool delete = false)
    {
        solution = null;

        if (!Resolve(ingested, ref ingested.Comp))
            return false;

        var ev = new EdibleEvent(user, delete);
        RaiseLocalEvent(ingested, ref ev);

        ev.Solution = solution;

        return ev.Cancelled && solution != null;
    }

    #endregion
}
