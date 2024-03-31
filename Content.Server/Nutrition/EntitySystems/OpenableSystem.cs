using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// Provides API for openable food and drinks, handles opening on use and preventing transfer when closed.
/// </summary>
public sealed class OpenableSystem : SharedOpenableSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenableComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
    }

    private void OnTransferAttempt(EntityUid uid, OpenableComponent comp, SolutionTransferAttemptEvent args)
    {
        if (!comp.Opened)
        {
            // message says its just for drinks, shouldn't matter since you typically dont have a food that is openable and can be poured out
            args.Cancel(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", uid)));
        }
    }
}
