using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Shared.Dragon;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Dragon;

public sealed partial class DragonSystem
{
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

    private void OnRiftRoundEnd(RoundEndTextAppendEvent args)
    {
        if (EntityQuery<DragonComponent>().Count() == 0)
            return;

        args.AddLine(Loc.GetString("dragon-round-end-summary"));

        var query = EntityQueryEnumerator<DragonComponent>();
        while (query.MoveNext(out var uid, out var dragon))
        {
            var met = RiftsMet(dragon);

            if (TryComp<ActorComponent>(uid, out var actor))
            {
                args.AddLine(Loc.GetString("dragon-round-end-dragon-player", ("name", uid), ("count", met), ("player", actor.PlayerSession)));
            }
            else
            {
                args.AddLine(Loc.GetString("dragon-round-end-dragon", ("name", uid), ("count", met)));
            }
        }
    }
}
