using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Public API for Ingestion System so you can build your own form of ingestion system.
/// </summary>
public sealed partial class IngestionSystem
{
    public const float MaxFeedDistance = 1.0f; // We should really have generic interaction ranges like short, medium, long and use those instead...
    // BodySystem has no way of telling us where the mouth is so we're making some assumptions.
    public const SlotFlags DefaultFlags = SlotFlags.HEAD | SlotFlags.MASK;

    #region Ingestion

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

    public bool CanIngest(EntityUid user, EntityUid target, EntityUid ingested)
    {
        return CanIngest(user, target, ingested, out _);
    }

    /// <summary>
    ///     Checks if we can feed an edible solution to a target and returns the solution.
    /// </summary>
    /// <param name="user">The one doing the feeding</param>
    /// <param name="target">The one being fed.</param>
    /// <param name="ingested">The food item being eaten.</param>
    /// <param name="solution">The solution we will be consuming from.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity</returns>
    public bool CanIngest(EntityUid user,
        EntityUid target,
        EntityUid ingested,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;

        if (!HasMouthAvailable(user, target))
            return false;

        // If we don't have the tools to eat we can't eat.
        return TryAccessSolution(ingested, user, out solution);
    }

    #endregion

    #region EdibleComponent

    public int GetUsesRemaining(Entity<EdibleComponent?> ingested, FixedPoint2 splitVol)
    {
        if (!Resolve(ingested, ref ingested.Comp))
            return 0;

        return GetUsesRemaining(ingested, ingested.Comp.Solution, splitVol);
    }

    public FixedPoint2 EdibleVolume(Entity<EdibleComponent> entity)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
            return FixedPoint2.Zero;

        return solution.Volume;
    }

    public bool IsEmpty(Entity<EdibleComponent> entity)
    {
        return EdibleVolume(entity) == FixedPoint2.Zero;
    }

    // TODO: This method for NPCs
    public float TotalHunger()
    {
        return 0f;
    }

    // TODO: This method for NPCs
    public float TotalHydration()
    {
        return 0f;
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

        solution = ev.Solution;

        return !ev.Cancelled && solution != null;
    }

    /// <summary>
    /// Estimate the number of bites this food has left, based on how much food solution there is and how much of it to eat per bite.
    /// </summary>
    public int GetUsesRemaining(EntityUid uid, string solutionName, FixedPoint2 splitVol)
    {
        if (!_solutionContainer.TryGetSolution(uid, solutionName, out _, out var solution) || solution.Volume == 0)
            return 0;

        return Math.Max(1, (int) Math.Ceiling((solution.Volume / splitVol).Float()));
    }

    #endregion

    #region Edible Types

    // TODO: GET A NOUNS/VERBS METHOD.
    // TODO: NOUNS/VERBS METHOD
    // TODO: DON'T FUCKING FORGET

    /// <summary>
    /// Tries to get the ingestion verbs for a given user entity and ingestible entity
    /// </summary>
    /// <param name="user">The one getting the verbs who would be doing the eating.</param>
    /// <param name="ingested"></param>
    /// <param name="type"></param>
    /// <param name="verb"></param>
    /// <returns></returns>
    public bool TryGetIngestionVerb(EntityUid user, EntityUid ingested, EdibleType type, [NotNullWhen(true)] out AlternativeVerb? verb)
    {
        verb = null;

        // We want to see if we can ingest this item, but we don't actually want to ingest it.
        if (!TryIngest(user, user, ingested, false))
            return false;

        SpriteSpecifier icon;
        string text;

        switch (type)
        {
            case EdibleType.Food:
                icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png"));
                text = Loc.GetString("food-system-verb-eat");
                break;
            case EdibleType.Drink:
                icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drink.svg.192dpi.png"));
                text = Loc.GetString("drink-system-verb-drink");
                break;
            default:
                Log.Error($"Entity {ToPrettyString(ingested)} doesn't have a proper Nutrition type or its verb isn't properly set up.");
                return false;
        }

        verb = new()
        {
            Act = () =>
            {
                TryIngest(user, user, ingested);
            },
            Icon = icon,
            Text = text,
            Priority = -1
        };

        return true;
    }

    public string GetStringNoun(EdibleComponent component)
    {
        switch (component.EdibleType)
        {
            case EdibleType.Food:
                return Loc.GetString("food");
            case EdibleType.Drink:
                return Loc.GetString("drink");
            default:
                Log.Error($"EdibleType {component.EdibleType} doesn't have a proper noun associated with it.");
                return Loc.GetString("edible");
        }
    }

    public string GetStringVerb(EdibleComponent component)
    {
        switch (component.EdibleType)
        {
            case EdibleType.Food:
                return Loc.GetString("eat");
            case EdibleType.Drink:
                return Loc.GetString("drink");
            default:
                Log.Error($"EdibleType {component.EdibleType} doesn't have a proper verb associated with it.");
                return Loc.GetString("edible");
        }
    }

    #endregion
}
