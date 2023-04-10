using System.Linq;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared.Dragon;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Dragon;

public sealed partial class DragonSystem
{
    public override string Prototype => "Dragon";

    private int RiftsMet(DragonComponent component)
    {
        var finished = 0;

        foreach (var rift in component.Rifts)
        {
            if (!TryComp<DragonRiftComponent>(rift, out var drift) ||
                drift.State != DragonRiftState.Finished)
                continue;

            finished++;
        }

        return finished;
    }

    public override void Started()
    {
        var spawnLocations = EntityManager.EntityQuery<MapGridComponent, TransformComponent>().ToList();

        if (spawnLocations.Count == 0)
            return;

        var location = _random.Pick(spawnLocations);
        Spawn("MobDragon", location.Item2.MapPosition);
    }

    public override void Ended()
    {
        return;
    }

    private void OnRiftRoundEnd(RoundEndTextAppendEvent args)
    {
        if (!RuleAdded)
            return;

        var dragons = EntityQuery<DragonComponent>(true).ToList();

        if (dragons.Count == 0)
            return;

        args.AddLine(Loc.GetString("dragon-round-end-summary"));

        foreach (var dragon in EntityQuery<DragonComponent>(true))
        {
            var met = RiftsMet(dragon);

            if (TryComp<ActorComponent>(dragon.Owner, out var actor))
            {
                args.AddLine(Loc.GetString("dragon-round-end-dragon-player", ("name", dragon.Owner), ("count", met), ("player", actor.PlayerSession)));
            }
            else
            {
                args.AddLine(Loc.GetString("dragon-round-end-dragon", ("name", dragon.Owner), ("count", met)));
            }
        }
    }
}
