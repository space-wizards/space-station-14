using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;

using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Tools;

public sealed class ToolSystem : SharedToolSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateWelders(frameTime);
    }

    //todo move to shared once you can remove reagents from shared without it freaking out.
    private void UpdateWelders(float frameTime)
    {
        var query = EntityQueryEnumerator<WelderComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var welder, out var solutionContainer))
        {
            if (!welder.Enabled)
                continue;

            welder.WelderTimer += frameTime;

            if (welder.WelderTimer < welder.WelderUpdateTimer)
                continue;

            if (!SolutionContainerSystem.TryGetSolution((uid, solutionContainer), welder.FuelSolutionName, out var solutionComp, out var solution))
                continue;

            SolutionContainerSystem.RemoveReagent(solutionComp.Value, welder.FuelReagent, welder.FuelConsumption * welder.WelderTimer);

            if (solution.GetTotalPrototypeQuantity(welder.FuelReagent) <= FixedPoint2.Zero)
            {
                ItemToggle.Toggle(uid, predicted: false);
            }

            Dirty(uid, welder);
            welder.WelderTimer -= welder.WelderUpdateTimer;
        }
    }
}

