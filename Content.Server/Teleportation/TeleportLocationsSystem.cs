using Content.Server.Warps;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.UserInterface;

namespace Content.Server.Teleportation;

public sealed partial class TeleportLocationsSystem : SharedTeleportLocationsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationRequestPointsMessage>(OnTeleportLocationRequest);
    }

    private void OnInit(Entity<TeleportLocationsComponent> ent, ref MapInitEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void OnTeleportLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationRequestPointsMessage args)
    {
        UpdateTeleportPoints(ent);
    }

    private void UpdateTeleportPoints(Entity<TeleportLocationsComponent> ent)
    {
        var allEnts = AllEntityQuery<WarpPointComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp))
        {
            if (warpPointComp.GhostOnly || string.IsNullOrWhiteSpace(warpPointComp.Location))
                continue;

            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.Location, GetNetEntity(warpEnt)));
        }

        Dirty(ent);
    }
}
