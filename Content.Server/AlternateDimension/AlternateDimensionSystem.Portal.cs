using Content.Shared.AlternateDimension;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.AlternateDimension;

public sealed partial class AlternateDimensionSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    private void InitializePortal()
    {
        SubscribeLocalEvent<AlternateDimensionAutoPortalComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AlternateDimensionAutoPortalComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid is null)
            return;

        //If in alternate dimension - try link with original world
        if (HasComp<AlternateDimensionGridComponent>(xform.GridUid.Value))
        {
            TryCreateAndLinkPortal(ent, GetOriginalRealityCoordinates(ent));
            return;
        }

        //If in real world - try found alternate dimension and link with it
        TryCreateAndLinkPortal(ent, GetAlternateRealityCoordinates(ent, ent.Comp.TargetDimension));
    }

    private void TryCreateAndLinkPortal(Entity<AlternateDimensionAutoPortalComponent> ent, EntityCoordinates? coord)
    {
        if (coord is null)
            return;

        var otherEnt = SpawnAtPosition(ent.Comp.OtherSidePortal, coord.Value);
        _link.TryLink(otherEnt, ent, true);

        //Make sure
        if (TryComp<PortalComponent>(ent, out var portal1))
        {
            portal1.CanTeleportToOtherMaps = true;
        }
        if (TryComp<PortalComponent>(otherEnt, out var portal2))
        {
            portal2.CanTeleportToOtherMaps = true;
        }
    }
}
