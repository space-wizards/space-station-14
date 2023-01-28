using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Random;

public sealed class EmoteRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

    }

    private void DoRandom(EntityUid uid, EmoteRandomComponent component)
    {
        var meta = MetaData(uid);
    }

    public override void

     Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var emoterandomcomp in EntityQuery<EmoteRandomComponent>())
        {
            emoterandomcomp.LastEmoteRandomCooldown -= frameTime;


            if (_timing.CurTime <= emoterandomcomp.EmoteOnRandomChance)
                continue;

            emoterandomcomp.EmoteOnRandomChance += TimeSpan.FromSeconds(emoterandomcomp.EmoteRandomAttempt);
            if (!_robustRandom.Prob(emoterandomcomp.EmoteRandomChance))
                continue;

            DoRandom(emoterandomcomp.Owner, emoterandomcomp);
        }

    }
}
