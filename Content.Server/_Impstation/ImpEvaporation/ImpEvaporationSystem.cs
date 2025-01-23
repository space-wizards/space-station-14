using Content.Shared._Impstation.ImpEvaporation;
using Content.Shared.Fluids;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Coordinates;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Timing;
using Content.Shared.Fluids.Components;
using Robust.Shared.Network;


namespace Content.Server._Impstation.ImpEvaporation;

/// <summary>
/// <inheritdoc/>
/// </summary>
public sealed partial class ImpEvaporationSystem : SharedImpEvaporationSystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        TickEvaporation();
    }
}
