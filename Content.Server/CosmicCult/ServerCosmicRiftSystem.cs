using Content.Server.Research.Systems;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server.CosmicCult;

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

    protected override void OnCollide(Entity<CosmicRiftComponent> ent, ref EndCollideEvent args)
    {
        base.OnCollide(ent, ref args);

        if (ent.Comp.CurrentHits >= ent.Comp.MaxHits)
        {
            if (Transform(ent).GridUid is not { } grid)
                return;

            var vfx = Spawn(CosmicCultSystem.GenericVfx, Transform(ent).Coordinates);
            _audio.PlayPredicted(ent.Comp.ExpungeSound, vfx, vfx);
            QueueDel(ent);

            var servers = _research.GetServers(grid);
            foreach (var server in servers)
            {
                _research.ModifyServerPoints(server, 1200);
            }
        }
    }
}
