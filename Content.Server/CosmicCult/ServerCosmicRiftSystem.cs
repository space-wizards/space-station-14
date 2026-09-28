using Content.Server.Research.Systems;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;

namespace Content.Server.CosmicCult;

/// <inheritdoc/>
public sealed partial class ServerCosmicRiftSystem : CosmicRiftSystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var riftQuery = EntityQueryEnumerator<CosmicRiftComponent>();
        while (riftQuery.MoveNext(out var uid, out var comp))
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
        if (!HasComp<TransformComponent>(ent.Comp.GridUid))
            return;

        var servers = _research.GetServers(ent.Comp.GridUid.Value);
        foreach (var server in servers)
        {
            _research.ModifyServerPoints(server, ent.Comp.ResearchPoints);
        }
    }
}
