using Content.Shared.StationTeleporter;
using Content.Shared.StationTeleporter.Components;

namespace Content.Server.StationTeleporter.Systems;

public sealed class StationTeleporterSystem : SharedStationTeleporterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTeleporterConsoleComponent, MapInitEvent>(OnConsoleInit);
    }

    private void OnConsoleInit(Entity<StationTeleporterConsoleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AutoLinkKey is null || ent.Comp.AutoLinkChipsProto is null)
            return;

        var query = EntityQueryEnumerator<StationTeleporterComponent>();
        while (query.MoveNext(out var teleporterUid, out var teleporter))
        {
            if (teleporter.AutoLinkKey is null || ent.Comp.AutoLinkKey != teleporter.AutoLinkKey)
                continue;

            //Spawn chip inside this console
            var chipEnt = SpawnInContainerOrDrop(ent.Comp.AutoLinkChipsProto, ent, ent.Comp.ChipStorageName);
            if (TryComp<TeleporterChipComponent>(chipEnt, out var chipComp))
            {
                ConnectChipToTeleporter((chipEnt, chipComp), (teleporterUid, teleporter));
            }
        }
    }
}
