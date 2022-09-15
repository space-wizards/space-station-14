using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;

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
///     Raised directed at the food after a successful feed do-after.
/// </summary>
public sealed class FeedEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly FoodComponent Food;
    public readonly Solution FoodSolution;
    public readonly string FlavorMessage;
    public readonly List<UtensilComponent> Utensils;

    public FeedEvent(EntityUid user, FoodComponent food, Solution foodSolution, string flavorMessage, List<UtensilComponent> utensils)
    {
        User = user;
        Food = food;
        FoodSolution = foodSolution;
        FlavorMessage = flavorMessage;
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
public sealed class DrinkEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly DrinkComponent Drink;
    public readonly Solution DrinkSolution;
    public readonly string FlavorMessage;

    public DrinkEvent(EntityUid user, DrinkComponent drink, Solution drinkSolution, string flavorMessage)
    {
        User = user;
        Drink = drink;
        DrinkSolution = drinkSolution;
        FlavorMessage = flavorMessage;
    }
}

/// <summary>
///     Raised directed at the food after a failed force-dink do-after.
/// </summary>
public sealed class DrinkCancelledEvent : EntityEventArgs
{
    public readonly DrinkComponent Drink;

    public DrinkCancelledEvent(DrinkComponent drink)
    {
        Drink = drink;
    }
}
