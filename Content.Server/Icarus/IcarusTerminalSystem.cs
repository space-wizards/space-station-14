using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Icarus;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Icarus;

/// <summary>
/// Handle Icarus activation terminal
/// </summary>
public sealed class IcarusTerminalSystem : EntitySystem
{
    private const string IcarusBeamPrototypeId = "IcarusBeam";

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IcarusBeamSystem _icarusSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQuery<IcarusTerminalComponent>();
        foreach (var terminal in query)
        {
            switch (terminal.Status)
            {
                case IcarusTerminalStatus.FIRE_PREPARING:
                    TickTimer(terminal, frameTime);
                    break;
                case IcarusTerminalStatus.COOLDOWN:
                    TickCooldown(terminal, frameTime);
                    break;
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IcarusTerminalComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IcarusTerminalComponent, ItemSlotChangedEvent>(OnItemSlotChanged);

        // UI events
        SubscribeLocalEvent<IcarusTerminalComponent, IcarusTerminalFireMessage>(OnFireButtonPressed);
    }

    private void OnInit(EntityUid uid, IcarusTerminalComponent component, ComponentInit args)
    {
        component.RemainingTime = component.Timer;
        UpdateStatus(component);
        UpdateUserInterface(component);
    }

    private void OnItemSlotChanged(EntityUid uid, IcarusTerminalComponent component, ref ItemSlotChangedEvent args)
    {
        UpdateStatus(component);
        UpdateUserInterface(component);
    }

    private void OnFireButtonPressed(EntityUid uid, IcarusTerminalComponent component, IcarusTerminalFireMessage args)
    {
        Fire(component);
    }

    private void Fire(IcarusTerminalComponent component)
    {
        if (component.Status == IcarusTerminalStatus.FIRE_PREPARING)
            return;

        component.RemainingTime = component.Timer;
        component.Status = IcarusTerminalStatus.FIRE_PREPARING;

        _chatSystem.DispatchStationAnnouncement(component.Owner,
            Loc.GetString("icarus-fire-announcement", ("seconds", component.Timer)),
            Loc.GetString("icarus-announce-sender"),
            false,
            Color.Red);
        SoundSystem.Play(component.AlertSound.GetSound(), Filter.Broadcast());
    }

    private void UpdateStatus(IcarusTerminalComponent component)
    {
        switch (component.Status)
        {
            case IcarusTerminalStatus.AWAIT_DISKS:
                if (IsAccessGranted(component.Owner))
                    Authorize(component);
                break;
            case IcarusTerminalStatus.FIRE_READY:
            {
                if (!IsAccessGranted(component.Owner))
                {
                    component.Status = IcarusTerminalStatus.AWAIT_DISKS;
                }
                break;
            }
        }
    }

    private void UpdateUserInterface(IcarusTerminalComponent component)
    {
        _userInterfaceSystem.TrySetUiState(component.Owner, IcarusTerminalUiKey.Key, new IcarusTerminalUiState(
            component.Status,
            (int) component.RemainingTime,
            (int) component.CooldownTime)
        );
    }

    private bool IsAccessGranted(EntityUid uid)
    {
        return Comp<ItemSlotsComponent>(uid).Slots.Values.All(v => v.HasItem);
    }

    private bool CanFire(EntityUid uid, IcarusTerminalComponent component)
    {
        return IsAccessGranted(uid) &&
               component.Status == IcarusTerminalStatus.FIRE_READY;
    }

    private void Authorize(IcarusTerminalComponent component)
    {
        component.Status = IcarusTerminalStatus.FIRE_READY;

        SoundSystem.Play(component.AccessGrantedSound.GetSound(), Filter.Pvs(component.Owner), component.Owner);

        if (!component.AuthorizationNotified)
        {
            _chatSystem.DispatchStationAnnouncement(component.Owner, Loc.GetString("icarus-authorized-announcement"),
                playDefaultSound: false); // TODO: Just pass custom sound path after PR accepting
            SoundSystem.Play("/Audio/Misc/notice1.ogg",
                Filter.Broadcast());
            component.AuthorizationNotified = true;
        }
    }

    private void TickCooldown(IcarusTerminalComponent component, float frameTime)
    {
        component.CooldownTime -= frameTime;
        if (component.CooldownTime <= 0)
        {
            component.CooldownTime = 0;
            component.Status = IcarusTerminalStatus.AWAIT_DISKS;
            UpdateStatus(component);
        }

        UpdateUserInterface(component);
    }

    private void TickTimer(IcarusTerminalComponent component, float frameTime)
    {
        component.RemainingTime -= frameTime;
        if (component.RemainingTime <= 0)
        {
            component.RemainingTime = 0;
            ActivateBeamOnStation(component);
        }

        UpdateUserInterface(component);
    }

    private void ActivateBeamOnStation(IcarusTerminalComponent component)
    {
        component.Status = IcarusTerminalStatus.COOLDOWN;
        component.CooldownTime = component.Cooldown;

        SoundSystem.Play(component.FireSound.GetSound(), Filter.Broadcast());
        FireBeam(GetStationArea());
    }

    public MapCoordinates FireBeam(Box2 area)
    {
        TryGetBeamSpawnLocation(area, out var coords, out var offset);
        Logger.DebugS("icarus", $"Try spawn beam on coords: {coords.ToString()}");
        var entUid = Spawn(IcarusBeamPrototypeId, coords);
        _icarusSystem.LaunchInDirection(entUid, -offset.Normalized);
        return coords;
    }

    private void TryGetBeamSpawnLocation(Box2 area, out MapCoordinates coords,
        out Vector2 offset)
    {
        coords = MapCoordinates.Nullspace;
        offset = Vector2.Zero;

        var center = area.Center;
        var distance = (area.TopRight - center).Length;
        var angle = new Angle(_robustRandom.NextFloat() * MathF.Tau);

        offset = angle.RotateVec(new Vector2(distance, 0));
        coords = new MapCoordinates(center + offset, _gameTicker.DefaultMap);
    }

    private Box2 GetStationArea()
    {
        var areas = _stationSystem.Stations.SelectMany(x =>
            Comp<StationDataComponent>(x).Grids.Select(x => _mapManager.GetGridComp(x).Grid.WorldAABB)).ToArray();
        var stationArea = areas[0];

        for (var i = 1; i < areas.Length; i++)
        {
            stationArea.Union(areas[i]);
        }

        return stationArea;
    }
}
