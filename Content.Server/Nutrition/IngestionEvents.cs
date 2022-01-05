using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server.Nutrition;

/// <summary>
///     Raised directed at the consumer when attempting to ingest something.
/// </summary>
public sealed class IngestionAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    ///     The equipment that is blocking consumption. Should only be non-null if the event was canceled.
    /// </summary>
    public EntityUid? Blocker = null;
}

/// <summary>
///     Raised directed at the food after a successful force-feed do-after.
/// </summary>
public sealed class ForceFeedEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly FoodComponent Food;
    public readonly Solution FoodSolution;
    public readonly List<UtensilComponent> Utensils;

    public ForceFeedEvent(EntityUid user, FoodComponent food, Solution foodSolution, List<UtensilComponent> utensils)
    {
        User = user;
        Food = food;
        FoodSolution = foodSolution;
        Utensils = utensils;
    }
}

/// <summary>
///     Raised directed at the food after a failed force-feed do-after.
/// </summary>
public sealed class ForceFeedCancelledEvent : EntityEventArgs
{
    public readonly FoodComponent Food;

    public ForceFeedCancelledEvent(FoodComponent food)
    {
        Food = food;
    }
}

/// <summary>
///     Raised directed at the drink after a successful force-drink do-after.
/// </summary>
public sealed class ForceDrinkEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly DrinkComponent Drink;
    public readonly Solution DrinkSolution;

    public ForceDrinkEvent(EntityUid user, DrinkComponent drink, Solution drinkSolution)
    {
        User = user;
        Drink = drink;
        DrinkSolution = drinkSolution;
    }
}

/// <summary>
///     Raised directed at the food after a failed force-dink do-after.
/// </summary>
public sealed class ForceDrinkCancelledEvent : EntityEventArgs
{
    public readonly DrinkComponent Drink;

    public ForceDrinkCancelledEvent(DrinkComponent drink)
    {
        Drink = drink;
    }
}
