using Content.Shared.Hands.Components;
using Content.Shared.Stunnable;
using static Content.Shared.Stunnable.SharedStunSystem;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// This is for events that don't affect normal hand functions but do care about hands.
/// </summary>
public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeEventListeners()
    {
        SubscribeLocalEvent<HandsComponent, StandUpArgsEvent>(OnStandupArgs);
    }

    /// <summary>
    /// Reduces the time it takes to stand up based on the number of hands we have available.
    /// </summary>
    private void OnStandupArgs(Entity<HandsComponent> ent, ref StandUpArgsEvent args)
    {
        if (!HasComp<KnockedDownComponent>(ent) || !TryCountEmptyHands(ent.Owner, out var hands))
            return;

        args.DoAfterTime *= (float)ent.Comp.Count / (hands.Value + ent.Comp.Count);
    }
}
