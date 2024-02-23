using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedDrinkSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrinkComponent, AttemptShakeEvent>(CancelIfEmpty);
    }

    protected void CancelIfEmpty(EntityUid uid, DrinkComponent component, AttemptShakeEvent args)
    {
        if (IsEmpty(uid, component))
            args.Cancel();
    }

    protected FixedPoint2 DrinkVolume(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out _, out var sol))
            return FixedPoint2.Zero;

        return sol.Volume;
    }

    protected bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        return DrinkVolume(uid, component) <= 0;
    }
}
