using Content.Shared.Revolutionary.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Revolutionary;

// DS14-start
public abstract class SharedRevolutionarySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
    }

    private static void OnComponentGetStateAttempt(
        EntityUid uid,
        RevolutionaryComponent component,
        ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = args.Player != null;
    }

    private static void OnComponentGetStateAttempt(
        EntityUid uid,
        HeadRevolutionaryComponent component,
        ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = args.Player != null;
    }
}
// DS14-end
