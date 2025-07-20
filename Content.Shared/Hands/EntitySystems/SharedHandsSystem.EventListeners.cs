using Content.Shared.Hands.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// This is for events that don't affect normal hand functions but do care about hands.
/// </summary>
public abstract partial class SharedHandsSystem
{
    // TODO: Both of these values should be based on an entity's size or something.
    // Mininum weight for modifiers
    private static readonly int MinWeight = 2;
    // Maximum adjusted weight (so weight minus minweight) for maximum penalty
    private static readonly float MaxAdjustedWeight = 16f;
    private void InitializeEventListeners()
    {
        SubscribeLocalEvent<HandsComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockdownRefresh);
    }

    /// <summary>
    /// Reduces the time it takes to stand up based on the number of hands we have available.
    /// </summary>
    private void OnGetStandUpTime(Entity<HandsComponent> ent, ref GetStandUpTimeEvent time)
    {
        if (!HasComp<KnockedDownComponent>(ent))
            return;

        var hands = CountFreeHands(ent.Owner);

        if (hands == 0)
            return;

        time.DoAfterTime *= (float)ent.Comp.Count / (hands + ent.Comp.Count);
    }

    private void OnKnockdownRefresh(Entity<HandsComponent> ent, ref KnockedDownRefreshEvent args)
    {
        var free = CountFreeHands((ent, ent.Comp));
        // If all our hands are empty, full move speed!
        if (free == ent.Comp.Count)
            return;

        var weight = CountHeldItemsWeight((ent, ent.Comp));

        // If we're below the weight where we start taking speed penalties, just fuggetabout it!
        if (weight <= MinWeight)
            return;

        // If all our hands are free or weight is less than min weight we shouldn't be here.
        // Effectively We get two values:
        // One is the total weight minus min weight
        // And the other is our hand count minus free hands.
        // We multiply these values together to get an encumbrance, if you have more hands free you can better manage the weight you're carrying.
        // Then we divide by the max adjusted weight and clamp to get our modifier.
        var modifier =  Math.Max(0f, 1f - (weight - MinWeight) * (ent.Comp.Count - free) / MaxAdjustedWeight);
        Log.Debug($"Appliyng a speed modifier of {modifier} to {ToPrettyString(ent)} from an item weight total of {weight} and empty hand count of {free}");

        args.SpeedModifier *= modifier;
    }
}
