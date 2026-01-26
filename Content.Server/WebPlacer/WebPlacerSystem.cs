using Content.Shared.Mobs.Systems;
using Content.Shared.WebPlacer;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.WebPlacer;

/// <inheritdoc />
public sealed class WebPlacerSystem : SharedWebPlacerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    // TODO NPCs using actions from HTN.
    /// <summary>
    /// NPC spiders will occasionally spawn webs.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WebPlacerComponent>();
        while (query.MoveNext(out var uid, out var webComp))
        {
            webComp.NextWebSpawn ??= _timing.CurTime + webComp.WebSpawnCooldown;

            if (_timing.CurTime < webComp.NextWebSpawn)
                continue;

            webComp.NextWebSpawn += webComp.WebSpawnCooldown;

            if (HasComp<ActorComponent>(uid)
                || _mobState.IsDead(uid)
                || !webComp.SpawnsWebsAsNonPlayer)
                continue;

            var transform = Transform(uid);
            TrySpawnWebs((uid, webComp), transform.Coordinates);
        }
    }
}
