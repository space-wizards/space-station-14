using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StationTeleporter.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.StationTeleporter;

/// <summary>
/// Handles <see cref="StationTeleporterComponent"/>, <see cref="StationTeleporterConsoleComponent"/> and
/// <see cref="TeleporterChipComponent"/> - linking teleporters together via chips recorded at a control console.
/// </summary>
public abstract partial class SharedStationTeleporterSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    [Dependency] private EntityQuery<LabelComponent> _labelQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        SubscribeLocalEvent<StationTeleporterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationTeleporterComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationTeleporterComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<TeleporterChipComponent, ExaminedEvent>(OnChipExamined);
    }

    private void RefreshAllOpenConsoles()
    {
        var query = EntityQueryEnumerator<StationTeleporterConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            UpdateUserInterface((uid, console));
        }
    }

    private void ToggleTeleporterLink(Entity<StationTeleporterConsoleComponent> ent, ref StationTeleporterClickMessage args)
    {
        if (!TryGetEntity(args.Teleporter, out var teleporter))
            return;

        if (!_power.IsPowered(teleporter.Value))
            return;

        // Not every linkable teleporter is a StationTeleporter (e.g. hand teleporter portals aren't),
        // so this component is optional here - only used for its LastLink/PortalColor bookkeeping.
        TryComp<StationTeleporterComponent>(teleporter.Value, out var stationTeleporterComponent);

        if (_link.GetLink(teleporter.Value, out var linkedTeleporter))
            //If the pressed teleporter is linked to another - cut this connection.
        {
            _link.TryUnlink(teleporter.Value, linkedTeleporter.Value);
            stationTeleporterComponent?.LastLink = null;
        }
        else //If the pressed teleporter is not connected to anything...
        {
            if (ent.Comp.SelectedTeleporter is null) //And the console doesn't have teleporter selected - select it.
            {
                ent.Comp.SelectedTeleporter = teleporter;
            }
            else //If we have selected teleporter - tie them together
            {
                if (ent.Comp.SelectedTeleporter != teleporter.Value &&
                    _power.IsPowered(ent.Comp.SelectedTeleporter.Value))
                {
                    // Set the console's portal color on both sides before linking, so OnLinkedChanged
                    // can pick it up when it reacts to the link actually succeeding.
                    stationTeleporterComponent?.PortalColor = ent.Comp.PortalColor;

                    if (TryComp<StationTeleporterComponent>(ent.Comp.SelectedTeleporter.Value, out var selectedComponent))
                        selectedComponent.PortalColor = ent.Comp.PortalColor;

                    if (_link.TryLink(teleporter.Value, ent.Comp.SelectedTeleporter.Value) && stationTeleporterComponent is not null)
                        stationTeleporterComponent.LastLink = ent.Comp.SelectedTeleporter.Value;
                }

                ent.Comp.SelectedTeleporter = null;
            }
        }
    }

    private void OnInteractUsing(Entity<StationTeleporterComponent> teleporter, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(args.Used, out var useDelayComp) && _useDelay.IsDelayed((args.Used, useDelayComp)))
            return;

        _useDelay.TryResetDelay(args.Used);

        if (!TryComp<TeleporterChipComponent>(args.Used, out var chip))
            return;

        ConnectChipToTeleporter((args.Used, chip), teleporter);
        RefreshAllOpenConsoles();

        _popup.PopupEntity(Loc.GetString("teleporter-console-chip-record"), teleporter, args.User);
        _audio.PlayPredicted(chip.RecordSound, args.Used, args.User);

        args.Handled = true;
    }

    protected void ConnectChipToTeleporter(Entity<TeleporterChipComponent> chip,
        Entity<StationTeleporterComponent> teleporter)
    {
        chip.Comp.ConnectedTeleporter = teleporter;

        chip.Comp.ConnectedName = _labelQuery.TryComp(teleporter, out var label)
            ? label.CurrentLabel ?? Loc.GetString("teleporter-name-unknown")
            : Loc.GetString("teleporter-name-unknown");

        Dirty(chip);
    }

    private void OnChipExamined(Entity<TeleporterChipComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.ConnectedTeleporter is not null
            ? Loc.GetString("teleporter-console-chip-examine-recorded", ("portal", ent.Comp.ConnectedName))
            : Loc.GetString("teleporter-console-chip-examine-null"));
    }

    private void OnLinkedChanged(Entity<StationTeleporterComponent> ent, ref LinkedEntityChangedEvent args)
    {
        var xform = Transform(ent);
        if (args.NewLinks.Count > 0)
        {
            _appearance.SetData(ent, TeleporterPortalVisuals.Color, ent.Comp.PortalColor);
            _ambient.SetAmbience(ent, true);
            _audio.PlayPvs(ent.Comp.LinkSound, xform.Coordinates);
        }
        else
        {
            _ambient.SetAmbience(ent, false);
            _audio.PlayPvs(ent.Comp.UnlinkSound, xform.Coordinates);
        }

        RefreshAllOpenConsoles();
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
            if (ent.Comp.LastLink is not null)
            {
                if (_link.GetLink(ent.Comp.LastLink.Value, out _))
                    ent.Comp.LastLink = null;
                else
                    _link.TryLink(ent, ent.Comp.LastLink.Value);
            }
        }

        RefreshAllOpenConsoles();
    }
}
