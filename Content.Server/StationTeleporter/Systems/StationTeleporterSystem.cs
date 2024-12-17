using Content.Server.Audio;
using Content.Server.Power.EntitySystems;
using Content.Server.StationTeleporter.Components;
using Content.Shared.Examine;
using Content.Shared.StationTeleporter;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.StationTeleporter.Systems;

public sealed class StationTeleporterSystem : SharedStationTeleporterSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSys = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTeleporterConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<StationTeleporterConsoleComponent, StationTeleporterClickMessage>(OnUIPortalClicked);

        SubscribeLocalEvent<StationTeleporterComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationTeleporterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationTeleporterComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<TeleporterChipComponent, MapInitEvent>(OnChipInit);
        SubscribeLocalEvent<TeleporterChipComponent, ExaminedEvent>(OnChipExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationTeleporterConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.NextUpdateTime > _timing.CurTime)
                continue;

            console.NextUpdateTime += console.UpdateFrequency;

            UpdateUserInterface((uid, console));
        }
    }

    private void OnChipExamined(Entity<TeleporterChipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ConnectedTeleporter is not null)
            args.PushMarkup(Loc.GetString("teleporter-console-chip-examine-recorded", ("portal", ent.Comp.ConnectedName)));
    }

    private void OnInteractUsing(Entity<StationTeleporterComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<TeleporterChipComponent>(args.Used, out var chip))
            return;

        if (chip.ConnectedTeleporter is not null)
        {
            _popup.PopupEntity(Loc.GetString("teleporter-console-chip-already-recorded"), ent, args.User);
            return;
        }

        chip.ConnectedTeleporter = ent;
        chip.ConnectedName = Loc.GetString(ent.Comp.TeleporterName);
        _popup.PopupEntity(Loc.GetString("teleporter-console-chip-record"), ent, args.User);
        _audio.PlayPvs(chip.RecordSound, args.Used);

        args.Handled = true;
    }

    private void OnUIPortalClicked(Entity<StationTeleporterConsoleComponent> ent, ref StationTeleporterClickMessage args)
    {
        ConsoleInteract(ent, ref args);
        UpdateUserInterface(ent);
    }

    private void ConsoleInteract(Entity<StationTeleporterConsoleComponent> ent, ref StationTeleporterClickMessage args)
    {
        var teleporter = GetEntity(args.Teleporter);

        if (teleporter is null)
            return;

        if (!_power.IsPowered(teleporter.Value))
            return;

        if (!TryComp<StationTeleporterComponent>(teleporter.Value, out var stationTeleporterComponent))
            return;

        if (_link.GetLink(teleporter.Value, out var linkedTeleporter)) //If the pressed teleporter is linked to another - cut this connection.
        {
            _link.TryUnlink(teleporter.Value, linkedTeleporter.Value);
            stationTeleporterComponent.LastLink = null;
        }
        else //If the pressed teleporter is not connected to anything...
        {
            if (ent.Comp.SelectedTeleporter is null) //And the console doesn't have teleporter selected - select it.
            {
                ent.Comp.SelectedTeleporter = teleporter;
            }
            else // And we have a selected teleporter - tie them together.
            {
                if (ent.Comp.SelectedTeleporter != teleporter.Value && _power.IsPowered(ent.Comp.SelectedTeleporter.Value))
                {
                    if (_link.TryLink(teleporter.Value, ent.Comp.SelectedTeleporter.Value))
                        stationTeleporterComponent.LastLink = ent.Comp.SelectedTeleporter.Value;

                    _appearanceSys.SetData(teleporter.Value, TeleporterPortalVisual.Color, ent.Comp.PortalColor);
                    _appearanceSys.SetData(ent.Comp.SelectedTeleporter.Value, TeleporterPortalVisual.Color, ent.Comp.PortalColor);
                }
                ent.Comp.SelectedTeleporter = null;
            }
        }
    }

    private void OnLinkedChanged(Entity<StationTeleporterComponent> ent, ref LinkedEntityChangedEvent args)
    {
        if (args.NewLinks.Count > 0)
        {
            _ambient.SetAmbience(ent, true);
            _audio.PlayPvs(ent.Comp.LinkSound, ent);
        }
        else
        {
            _ambient.SetAmbience(ent, false);
            _audio.PlayPvs(ent.Comp.UnlinkSound, ent);
        }
    }

    private void OnPowerChanged(Entity<StationTeleporterComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            if (_link.GetLink(ent, out var secondLink))
                _link.TryUnlink(ent, secondLink.Value);
        }
        else
        {
            // We look for a portal from our “memory” and see if it's connected to anything. If not, we connect to it ourselves.
            if (ent.Comp.LastLink is null)
                return;

            if (_link.GetLink(ent.Comp.LastLink.Value, out _))
            {
                ent.Comp.LastLink = null;
                return;
            }

            _link.TryLink(ent, ent.Comp.LastLink.Value);
        }
    }

    private void OnUIOpened(Entity<StationTeleporterConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<StationTeleporterConsoleComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, StationTeleporterUIKey.Key))
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
            foreach (var chip in container.ContainedEntities)
            {
                if (!TryComp<TeleporterChipComponent>(chip, out var chipComp))
                    continue;

                if (!EntityManager.EntityExists(chipComp.ConnectedTeleporter))
                    continue;

                if (!TryComp<StationTeleporterComponent>(chipComp.ConnectedTeleporter, out var teleporter))
                    continue;

                if (cachedTeleporters.Contains(chipComp.ConnectedTeleporter.Value))
                    continue;

                var powered = _power.IsPowered(chipComp.ConnectedTeleporter.Value);

                _link.GetLink(chipComp.ConnectedTeleporter.Value, out var linkedTeleporter);
                EntityCoordinates? linkCoord = null;
                if (linkedTeleporter is not null)
                    linkCoord = Transform(linkedTeleporter.Value).Coordinates;

                cachedTeleporters.Add(chipComp.ConnectedTeleporter.Value);

                teleportersData.Add(
                    new(GetNetEntity(chipComp.ConnectedTeleporter.Value),
                        GetNetCoordinates(Transform(chipComp.ConnectedTeleporter.Value).Coordinates),
                        GetNetEntity(chipComp.ConnectedTeleporter.Value),
                        GetNetCoordinates(linkCoord),
                        Loc.GetString(teleporter.TeleporterName),
                        powered));
            }
        }
        _uiSystem.SetUiState(ent.Owner, StationTeleporterUIKey.Key, new StationTeleporterState(teleportersData, GetNetEntity(ent.Comp.SelectedTeleporter)));
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
            successLink = true;
            break;
        }
        if (!successLink && chip.Comp.DeleteOnFailedLink)
            QueueDel(chip);
    }
}
