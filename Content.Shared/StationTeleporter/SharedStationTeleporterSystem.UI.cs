using Content.Shared.Pinpointer;
using Content.Shared.StationTeleporter.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;

namespace Content.Shared.StationTeleporter;

public abstract partial class SharedStationTeleporterSystem
{
    private void InitializeUI()
    {
        SubscribeLocalEvent<StationTeleporterConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<StationTeleporterConsoleComponent, StationTeleporterClickMessage>(OnUIPortalClicked);
    }

    private void OnUIOpened(Entity<StationTeleporterConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnUIPortalClicked(Entity<StationTeleporterConsoleComponent> ent,
        ref StationTeleporterClickMessage args)
    {
        ConsoleInteract(ent, ref args);
        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<StationTeleporterConsoleComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, StationTeleporterConsoleUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(ent);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        //Send data
        List<StationTeleporterStatus> teleportersData = new();
        var cachedTeleporters = new List<EntityUid>(); //Prevent UI teleporters dublication

        if (_container.TryGetContainer(ent, ent.Comp.ChipStorageName, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                AddTeleportersFromChips(entity, ref teleportersData, ref cachedTeleporters);
                AddPortalsFromHandTeleporter(entity, ref teleportersData, ref cachedTeleporters);
            }
        }

        _uiSystem.SetUiState(ent.Owner,
            StationTeleporterConsoleUIKey.Key,
            new StationTeleporterState(teleportersData, GetNetEntity(ent.Comp.SelectedTeleporter)));
    }

    private void AddTeleportersFromChips(EntityUid ent,
        ref List<StationTeleporterStatus> teleportersData,
        ref List<EntityUid> cachedTeleporters)
    {
        //Teleporter chips get portal links
        if (!TryComp<TeleporterChipComponent>(ent, out var chipComp))
            return;

        if (Deleted(chipComp.ConnectedTeleporter))
            return;

        if (chipComp.ConnectedTeleporter is null)
            return;

        if (cachedTeleporters.Contains(chipComp.ConnectedTeleporter.Value))
            return;

        var powered = _power.IsPowered(chipComp.ConnectedTeleporter.Value);

        _link.GetLink(chipComp.ConnectedTeleporter.Value, out var linkedTeleporter);
        EntityCoordinates? linkCoord = null;
        if (linkedTeleporter is not null)
            linkCoord = Transform(linkedTeleporter.Value).Coordinates;

        cachedTeleporters.Add(chipComp.ConnectedTeleporter.Value);

        var teleporterName = LabelQuery.TryComp(chipComp.ConnectedTeleporter.Value, out var label)
            ? label.CurrentLabel ?? Loc.GetString("teleporter-name-unknown")
            : Loc.GetString("teleporter-name-unknown");

        teleportersData.Add(
            new(GetNetEntity(chipComp.ConnectedTeleporter.Value),
                GetNetCoordinates(Transform(chipComp.ConnectedTeleporter.Value).Coordinates),
                GetNetCoordinates(linkCoord),
                Loc.GetString(teleporterName),
                powered));
    }

    private void AddPortalsFromHandTeleporter(EntityUid ent,
        ref List<StationTeleporterStatus> teleportersData,
        ref List<EntityUid> cachedTeleporters)
    {
        //RD handheld teleporter portals
        if (!TryComp<HandTeleporterComponent>(ent, out var handTeleporter))
            return;

        //First portal
        if (handTeleporter.FirstPortal is not null && EntityManager.EntityExists(handTeleporter.FirstPortal))
            AddPortal(handTeleporter.FirstPortal.Value, Loc.GetString("teleporter-name-rd-first"), ref teleportersData, ref cachedTeleporters);


        //Second portal
        if (handTeleporter.SecondPortal is not null && EntityManager.EntityExists(handTeleporter.SecondPortal))
            AddPortal(handTeleporter.SecondPortal.Value, Loc.GetString("teleporter-name-rd-second"), ref teleportersData, ref cachedTeleporters);
    }

    private void AddPortal(EntityUid ent,
        string name,
        ref List<StationTeleporterStatus> teleportersData,
        ref List<EntityUid> cachedTeleporters)
    {
        if (cachedTeleporters.Contains(ent))
            return;

        _link.GetLink(ent, out var linkedTeleporter);
        EntityCoordinates? linkCoord = null;
        if (linkedTeleporter is not null)
            linkCoord = Transform(linkedTeleporter.Value).Coordinates;

        teleportersData.Add(
            new StationTeleporterStatus(GetNetEntity(ent),
                GetNetCoordinates(Transform(ent).Coordinates),
                GetNetCoordinates(linkCoord),
                name,
                true));
    }
}
