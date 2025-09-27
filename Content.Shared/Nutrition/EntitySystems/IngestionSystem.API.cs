using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Public API for Ingestion System so you can build your own form of ingestion system.
/// </summary>
public sealed partial class IngestionSystem
{
    // List of prototypes that other components or systems might want.
    public static readonly ProtoId<EdiblePrototype> Food = "Food";
    public static readonly ProtoId<EdiblePrototype> Drink = "Drink";

    public const float MaxFeedDistance = 1.0f; // We should really have generic interaction ranges like short, medium, long and use those instead...
    // BodySystem has no way of telling us where the mouth is so we're making some assumptions.
    public const SlotFlags DefaultFlags = SlotFlags.HEAD | SlotFlags.MASK;

    #region Ingestion

    /// <summary>
    /// An entity is trying to ingest another entity in Space Station 14!!!
    /// </summary>
    /// <param name="user">The entity who is eating.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    /// <returns>Returns true if we are now ingesting the item.</returns>
    public bool TryIngest(EntityUid user, EntityUid ingested)
    {
        return TryIngest(user, user, ingested);
    }

    /// <inheritdoc cref="TryIngest(EntityUid,EntityUid)"/>
    /// <summary>Overload of TryIngest for if an entity is trying to make another entity ingest an entity</summary>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    public bool TryIngest(EntityUid user, EntityUid target, EntityUid ingested)
    {
        return AttemptIngest(user, target, ingested, true);
    }

    /// <summary>
    /// Checks if we can ingest a given entity without actually ingesting it.
    /// </summary>
    /// <param name="user">The entity doing the ingesting.</param>
    /// <param name="ingested">The ingested entity.</param>
    /// <returns>Returns true if it's possible for the entity to ingest this item.</returns>
    public bool CanIngest(EntityUid user, EntityUid ingested)
    {
        return AttemptIngest(user, user, ingested, false);
    }

    /// <summary>
    ///     Check whether we have an open pie-hole that's in range.
    /// </summary>
    /// <param name="user">The one performing the action</param>
    /// <param name="target">The target whose mouth is checked</param>
    /// <returns></returns>
    public bool HasMouthAvailable(EntityUid user, EntityUid target)
    {
        return HasMouthAvailable(user, target, DefaultFlags);
    }

    /// <inheritdoc cref="HasMouthAvailable(EntityUid, EntityUid)"/>
    /// Overflow which takes custom flags for a mouth being blocked, in case the entity has a mouth not on the face.
    public bool HasMouthAvailable(EntityUid user, EntityUid target, SlotFlags flags)
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

        if (attempt.Blocker != null)
            _popup.PopupClient(Loc.GetString("ingestion-remove-mask", ("entity", attempt.Blocker.Value)), target, user);

        return false;
    }

    /// <inheritdoc cref="CanConsume(EntityUid,EntityUid)"/>
    /// <param name="user">The entity that is consuming</param>
    /// <param name="ingested">The entity that is being consumed</param>
    public bool CanConsume(EntityUid user, EntityUid ingested)
    {
        return CanConsume(user, user, ingested, out _, out _);
    }

    /// <summary>
    ///     Checks if we can feed an edible solution from an entity to a target.
    /// </summary>
    /// <param name="user">The one doing the feeding</param>
    /// <param name="target">The one being fed.</param>
    /// <param name="ingested">The food item being eaten.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity</returns>
    public bool CanConsume(EntityUid user, EntityUid target, EntityUid ingested)
    {
        return CanConsume(user, target, ingested, out _, out _);
    }

    /// <inheritdoc cref="CanConsume(EntityUid,EntityUid,EntityUid)"/>
    /// <param name="user">The one doing the feeding</param>
    /// <param name="target">The one being fed.</param>
    /// <param name="ingested">The food item being eaten.</param>
    /// <param name="solution">The solution we will be consuming from.</param>
    /// <param name="time">The time it takes us to eat this entity if any.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity and also returns a solution</returns>
    public bool CanConsume(EntityUid user,
        EntityUid target,
        EntityUid ingested,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        out TimeSpan? time)
    {
        solution = null;
        time = null;

        if (!HasMouthAvailable(user, target))
            return false;

        // If we don't have the tools to eat we can't eat.
        return CanAccessSolution(ingested, user, out solution, out time);
    }

    #endregion

    #region EdibleComponent

    public void SpawnTrash(Entity<EdibleComponent> entity, EntityUid? user = null)
    {
        if (entity.Comp.Trash.Count == 0)
            return;

        var position = _transform.GetMapCoordinates(entity);
        var trashes = entity.Comp.Trash;
        var pickup = user != null && _hands.IsHolding(user.Value, entity, out _);

        foreach (var trash in trashes)
        {
            var spawnedTrash = EntityManager.PredictedSpawn(trash, position);

            // If the user is holding the item
            if (!pickup)
                continue;

            // Put the trash in the user's hand
            // I am 100% confident we don't need this check but rider gets made at me if it's not here.
            if (user != null)
                _hands.TryPickupAnyHand(user.Value, spawnedTrash);
        }
    }

    public void AddTrash(Entity<EdibleComponent> entity, List<EntProtoId> newTrash)
    {
        foreach (var trash in newTrash)
        {
            entity.Comp.Trash.Add(trash);
        }
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

    /// <summary>
    /// Gets the total metabolizable nutrition from an entity, checks first if we can metabolize it.
    /// If we can't then it's not worth any nutrition.
    /// </summary>
    /// <param name="entity">The consumed entity</param>
    /// <param name="consumer">The entity doing the consuming</param>
    /// <returns>The amount of nutrition the consumable is worth</returns>
    public float TotalNutrition(Entity<EdibleComponent?> entity, EntityUid consumer)
    {
        if (!CanIngest(consumer, entity))
            return 0f;

        return TotalNutrition(entity);
    }

    /// <summary>
    /// Gets the total metabolizable nutrition from an entity, assumes we can eat and metabolize it.
    /// </summary>
    /// <param name="entity">The consumed entity</param>
    /// <returns>The amount of nutrition the consumable is worth</returns>
    public float TotalNutrition(Entity<EdibleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return 0f;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _proto.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    // ignores any effect conditions, just cares about how much it can hydrate
                    if (effect is SatiateHunger hunger)
                    {
                        total += hunger.Factor * quantity.Quantity.Float();
                    }
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Gets the total metabolizable hydration from an entity, checks first if we can metabolize it.
    /// If we can't then it's not worth any hydration.
    /// </summary>
    /// <param name="entity">The consumed entity</param>
    /// <param name="consumer">The entity doing the consuming</param>
    /// <returns>The amount of hydration the consumable is worth</returns>
    public float TotalHydration(Entity<EdibleComponent?> entity, EntityUid consumer)
    {
        if (!CanIngest(consumer, entity))
            return 0f;

        return TotalNutrition(entity);
    }

    /// <summary>
    /// Gets the total metabolizable hydration from an entity, assumes we can eat and metabolize it.
    /// </summary>
    /// <param name="entity">The consumed entity</param>
    /// <returns>The amount of hydration the consumable is worth</returns>
    public float TotalHydration(Entity<EdibleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return 0f;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _proto.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    // ignores any effect conditions, just cares about how much it can hydrate
                    if (effect is SatiateThirst thirst)
                    {
                        total += thirst.Factor * quantity.Quantity.Float();
                    }
                }
            }
        }

        return total;
    }

    #endregion

    #region Solutions

    /// <summary>
    /// Checks if the item is currently edible.
    /// </summary>
    /// <param name="ingested">Entity being ingested</param>
    /// <param name="user">The entity trying to make the ingestion happening, not necessarily the one eating</param>
    /// <param name="solution">Solution we're returning</param>
    /// <param name="time">The time it takes us to eat this entity</param>
    public bool CanAccessSolution(Entity<SolutionContainerManagerComponent?> ingested,
        EntityUid user,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        out TimeSpan? time)
    {
        solution = null;
        time = null;

        if (!Resolve(ingested, ref ingested.Comp))
        {
            _popup.PopupClient(Loc.GetString("ingestion-try-use-is-empty", ("entity", ingested)), ingested, user);
            return false;
        }

        var ev = new EdibleEvent(user);
        RaiseLocalEvent(ingested, ref ev);

        solution = ev.Solution;
        time = ev.Time;

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

    /// <summary>
    /// Tries to get the ingestion verbs for a given user entity and ingestible entity
    /// </summary>
    /// <param name="user">The one getting the verbs who would be doing the eating.</param>
    /// <param name="ingested">Entity being ingested.</param>
    /// <param name="type">Edible prototype.</param>
    /// <param name="verb">Verb we're returning.</param>
    /// <returns>Returns true if we generated a verb.</returns>
    public bool TryGetIngestionVerb(EntityUid user, EntityUid ingested, [ForbidLiteral] ProtoId<EdiblePrototype> type, [NotNullWhen(true)] out AlternativeVerb? verb)
    {
        verb = null;

        // We want to see if we can ingest this item, but we don't actually want to ingest it.
        if (!CanIngest(user, ingested))
            return false;

        var proto = _proto.Index(type);

        verb = new()
        {
            Act = () =>
            {
                TryIngest(user, user, ingested);
            },
            Icon = proto.VerbIcon,
            Text = Loc.GetString(proto.VerbName),
            Priority = 2
        };

        return true;
    }

    /// <summary>
    /// Returns the most accurate edible prototype for an entity if one exists.
    /// </summary>
    /// <param name="entity">entity who's edible prototype we want</param>
    /// <returns>The best matching prototype if one exists.</returns>
    public ProtoId<EdiblePrototype>? GetEdibleType(Entity<EdibleComponent?> entity)
    {
        if (Resolve(entity, ref entity.Comp, false))
            return entity.Comp.Edible;

        var ev = new GetEdibleTypeEvent();
        RaiseLocalEvent(entity, ref ev);

        return ev.Type;
    }

    public string GetEdibleNoun(Entity<EdibleComponent?> entity)
    {
        if (Resolve(entity, ref entity.Comp, false))
            return GetProtoVerb(entity.Comp.Edible);

        var ev = new GetEdibleTypeEvent();
        RaiseLocalEvent(entity, ref ev);

        if (ev.Type == null)
            return Loc.GetString("edible-noun-edible");

        return GetProtoNoun(ev.Type.Value);
    }

    public string GetProtoNoun([ForbidLiteral] ProtoId<EdiblePrototype> proto)
    {
        var prototype = _proto.Index(proto);

        return GetProtoNoun(prototype);
    }

    public string GetProtoNoun(EdiblePrototype proto)
    {
        return Loc.GetString(proto.Noun);
    }

    public string GetEdibleVerb(Entity<EdibleComponent?> entity)
    {
        if (Resolve(entity, ref entity.Comp, false))
            return GetProtoVerb(entity.Comp.Edible);

        var ev = new GetEdibleTypeEvent();
        RaiseLocalEvent(entity, ref ev);

        if (ev.Type == null)
            return Loc.GetString("edible-verb-edible");

        return GetProtoVerb(ev.Type.Value);
    }

    public string GetProtoVerb([ForbidLiteral] ProtoId<EdiblePrototype> proto)
    {
        var prototype = _proto.Index(proto);

        return GetProtoVerb(prototype);
    }

    public string GetProtoVerb(EdiblePrototype proto)
    {
        return Loc.GetString(proto.Verb);
    }

    #endregion
}
