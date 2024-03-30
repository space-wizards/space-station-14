using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Medical.Circulatory.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Circulatory.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    //TODO: Cvar this!
    private TimeSpan _updateInterval = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BloodstreamComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var bloodstreamComp, out var solMan))
        {
            if (_gameTiming.CurTime < bloodstreamComp.NextUpdate)
                continue;
            bloodstreamComp.NextUpdate += _updateInterval;
            var bloodstream =
                new Entity<BloodstreamComponent, SolutionContainerManagerComponent>(uid, bloodstreamComp, solMan);
            TransferBloodToSpill(bloodstream);
        }
    }


    /// <inheritdoc/>
    public override void Initialize()
    {
        _updateInterval = TimeSpan.FromSeconds(1.0f);
        InitSolutions();
    }
}
