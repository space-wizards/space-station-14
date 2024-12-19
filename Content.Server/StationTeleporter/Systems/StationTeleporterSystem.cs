using Content.Shared.StationTeleporter;
using Content.Shared.StationTeleporter.Components;

namespace Content.Server.StationTeleporter.Systems;

public sealed class StationTeleporterSystem : SharedStationTeleporterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterChipComponent, MapInitEvent>(OnChipInit);
    }

    private void OnChipInit(Entity<TeleporterChipComponent> chip, ref MapInitEvent args)
    {
        if (chip.Comp.AutoLinkKey is null)
            return;

        var query = EntityQueryEnumerator<StationTeleporterComponent>();
        var successLink = false;
        while (query.MoveNext(out var uid, out var teleporters))
        {
            if (teleporters.AutoLinkKey is null || teleporters.AutoLinkKey != chip.Comp.AutoLinkKey)
                continue;

            chip.Comp.ConnectedTeleporter = uid;
            chip.Comp.ConnectedName = Loc.GetString(teleporters.TeleporterName);
            Dirty(chip);

            successLink = true;
            break;
        }

        if (!successLink)
        {
            chip.Comp.AutoLinkKey = null;
        }
    }
}
