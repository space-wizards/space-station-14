using Content.Server.Warps;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.UserInterface;

namespace Content.Server.Teleportation;

public sealed partial class TeleportLocationsSystem : SharedTeleportLocationsSystem
{
    public override void Initialize()
    {
        base.Initialize();

    }

    protected override void OnTeleportLocationsOpen(Entity<TeleportLocationsComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        base.OnTeleportLocationsOpen(ent, ref args);

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
