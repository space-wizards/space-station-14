using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StationTeleporter.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.StationTeleporter;

public abstract partial class SharedStationTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    protected EntityQuery<LabelComponent> LabelQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        LabelQuery = GetEntityQuery<LabelComponent>();

        SubscribeLocalEvent<StationTeleporterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationTeleporterComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationTeleporterComponent, InteractUsingEvent>(OnInteractUsing);

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

    private void ConsoleInteract(Entity<StationTeleporterConsoleComponent> ent, ref StationTeleporterClickMessage args)
    {
        var teleporter = GetEntity(args.Teleporter);

        if (teleporter is null)
            return;

        if (!_power.IsPowered(teleporter.Value))
            return;

        TryComp<StationTeleporterComponent>(teleporter.Value, out var stationTeleporterComponent);

        if (_link.GetLink(teleporter.Value,
                out var linkedTeleporter)) //If the pressed teleporter is linked to another - cut this connection.
        {
            _link.TryUnlink(teleporter.Value, linkedTeleporter.Value);
            if (stationTeleporterComponent is not null)
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
                if (ent.Comp.SelectedTeleporter != teleporter.Value &&
                    _power.IsPowered(ent.Comp.SelectedTeleporter.Value))
                {
                    if (_link.TryLink(teleporter.Value, ent.Comp.SelectedTeleporter.Value))
                    {
                        if (stationTeleporterComponent is not null)
                            stationTeleporterComponent.LastLink = ent.Comp.SelectedTeleporter.Value;
                    }

                    _appearance.SetData(teleporter.Value, TeleporterPortalVisuals.Color, ent.Comp.PortalColor);
                    _appearance.SetData(ent.Comp.SelectedTeleporter.Value,
                        TeleporterPortalVisuals.Color,
                        ent.Comp.PortalColor);
                }

                ent.Comp.SelectedTeleporter = null;
            }
        }
    }

    private void OnInteractUsing(Entity<StationTeleporterComponent> teleporter, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(args.Used, out var useDelayComp) &&
            _useDelay.IsDelayed((args.Used, useDelayComp)))
            return;
        _useDelay.TryResetDelay(args.Used);

        if (!TryComp<TeleporterChipComponent>(args.Used, out var chip))
            return;

        ConnectChipToTeleporter((args.Used, chip), teleporter);

        _popup.PopupPredicted(Loc.GetString("teleporter-console-chip-record"), teleporter, args.User);
        Audio.PlayPredicted(chip.RecordSound, args.Used, args.User);

        args.Handled = true;
    }

    protected void ConnectChipToTeleporter(Entity<TeleporterChipComponent> chip,
        Entity<StationTeleporterComponent> teleporter)
    {
        chip.Comp.ConnectedTeleporter = teleporter;

        chip.Comp.ConnectedName = LabelQuery.TryComp(teleporter, out var label)
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
            _ambient.SetAmbience(ent, true);
            Audio.PlayPvs(ent.Comp.LinkSound, xform.Coordinates);
        }
        else
        {
            _ambient.SetAmbience(ent, false);
            Audio.PlayPvs(ent.Comp.UnlinkSound, xform.Coordinates);
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
}
