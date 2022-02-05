using System.Collections.Generic;
using Content.Server.Fluids.Components;
using Content.Server.Kudzu;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Fluids.EntitySystems;

public class FluidSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private float _accumulatedTimeFrame = 0.0f;
    private readonly List<FluidSpreadState> _fluidSpread = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<FluidSpreaderComponent, ComponentAdd>(FluidSpreaderAdd);
    }

    private void FluidSpreaderAdd(EntityUid uid, FluidSpreaderComponent component, ComponentAdd args)
    {
        if (component.Enabled)
            _fluidSpread.Add(new FluidSpreadState(component.Owner));
    }

    public override void Update(float frameTime)
    {
        _accumulatedTimeFrame += frameTime;
        if (!(_accumulatedTimeFrame >= 1.0f))
            return;

        base.Update(frameTime);

        foreach (var _fluidSpread in _fluidSpread)
        {
            SpreadFluid(_fluidSpread);
        }
    }

    private void SpreadFluid(FluidSpreadState fluidSpread, TransformComponent? transformComponent = null,
        SpreaderComponent? spreaderComponent = null, PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(fluidSpread.Uid, ref transformComponent, ref spreaderComponent, ref puddleComponent, false)
            || spreaderComponent.Enabled
            || !_mapManager.TryGetGrid(transformComponent.GridID, out var grid))
            return;

        var puddlesToExpand = new List<PuddleComponent> { puddleComponent };
        var loop = puddlesToExpand.Count > 0 && fluidSpread.FluidRemaining.CurrentVolume > FixedPoint2.Zero;
        while (puddlesToExpand.Count > 0 )
        {
            
        }
    }
}

internal class FluidSpreadState
{
    internal EntityUid Uid;
    internal Solution FluidRemaining;

    public FluidSpreadState(EntityUid uid, Solution fluidRemaining)
    {
        Uid = uid;
        FluidRemaining = fluidRemaining;
    }
}