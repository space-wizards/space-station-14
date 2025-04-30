using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// This is for events that don't affect normal hand functions but do care about hands.
/// </summary>
public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeEventListeners()
    {
        SubscribeLocalEvent<HandsComponent, StandupAttemptEvent>(OnStandupAttempt);
        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockDownRefresh);
    }

    /// <summary>
    /// Reduces the time it takes to stand up based on the number of hands we have available.
    /// </summary>
    private void OnStandupAttempt(Entity<HandsComponent> ent, ref StandupAttemptEvent args)
    {
        if (!HasComp<KnockedDownComponent>(ent) || !TryCountEmptyHands(ent, out var hands))
            return;

        args.DoAfterTime *= (float)ent.Comp.Count / (hands.Value + ent.Comp.Count);
    }

    private void OnKnockDownRefresh(Entity<HandsComponent> ent,
        ref KnockedDownRefreshEvent args)
    {
        if (!HasComp<KnockedDownComponent>(ent) || !TryCountEmptyHands(ent, out var hands) && !hands.HasValue)
            return;

        args.SpeedModifier *= (float)(hands.Value + 1)/(ent.Comp.Count + 1); // TODO: Unhardcode this calculation a little bit
    }
}
