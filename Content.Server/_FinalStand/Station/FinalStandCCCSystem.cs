using Content.Server.RoundEnd;
using Content.Shared.Destructible;

namespace Content.Server._FinalStand.Station;

public sealed class FinalStandCCCSystem : EntitySystem
{
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FinalStandCCCComponent, DestructionEventArgs>(OnCCCDestroyed);
    }

    private void OnCCCDestroyed(EntityUid uid, FinalStandCCCComponent comp, DestructionEventArgs args)
    {
        Log.Info("[FinalStand] Central Command Console destroyed. Ending round.");
        _roundEnd.EndRound();
    }
}
