using Content.Server.Research.Systems;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.CosmicCult;

/// <inheritdoc/>
public sealed partial class ServerCosmicRiftSystem : CosmicRiftSystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shiftingQuery = EntityQueryEnumerator<CosmicRiftComponent>();
        while (shiftingQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.HitTimer is { } timer && Timing.CurTime >= timer)
            {
                comp.HitTimer = null;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CosmicRiftComponent> ent, ref ComponentShutdown args)
    {
        var servers = _research.GetServers(ent.Comp.GridUid);
        foreach (var server in servers)
        {
            _research.ModifyServerPoints(server, ent.Comp.ResearchPoints);
        }
    }
}
