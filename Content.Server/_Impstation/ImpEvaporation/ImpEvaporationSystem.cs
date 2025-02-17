using Content.Shared._Impstation.ImpEvaporation;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;


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
