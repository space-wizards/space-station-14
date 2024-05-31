using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;

using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Tools;

public sealed class ToolSystem : SharedToolSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void TurnOn(Entity<WelderComponent> entity, EntityUid? user)
    {
        base.TurnOn(entity, user);
        var xform = Transform(entity);
        if (xform.GridUid is { } gridUid)
        {
            var position = _transformSystem.GetGridOrMapTilePosition(entity.Owner, xform);
            _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, entity.Owner, true);
        }
    }

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

