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
        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockDownRefresh);
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

    private void OnKnockDownRefresh(Entity<HandsComponent> ent,
        ref KnockedDownRefreshEvent args)
    {
        if (!HasComp<KnockedDownComponent>(ent) || !TryCountEmptyHands(ent.Owner, out var hands) && !hands.HasValue)
            return;

        // TODO: Instead of this being based on hands, it should be based on the bulk of the items we're holding
        args.SpeedModifier *= (float)(hands.Value + 1)/(ent.Comp.Count + 1); // TODO: Unhardcode this calculation a little bit
    }
}
