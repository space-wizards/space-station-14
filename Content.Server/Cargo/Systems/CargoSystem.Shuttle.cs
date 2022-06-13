using Content.Server.Cargo.Components;
using Content.Server.GameTicking.Events;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.GameTicking;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly IMapLoader _loader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public MapId? CargoMap { get; private set; }

    // TODO: Store this on the component ya mong
    // ZTODO: Store cargo shuttle on station comp.

    /// <summary>
    /// Stores the cargo map coordinates for each shuttle.
    /// </summary>
    private readonly Dictionary<EntityUid, EntityCoordinates> _shuttles = new();

    #region Setup

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentInit>(OnCargoShuttleConsoleInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnCargoShuttleConsoleInit(EntityUid uid, CargoShuttleConsoleComponent component, ComponentInit args)
    {
        var station = _station.GetOwningStation(uid);

        _uiSystem.GetUiOrNull(uid, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                string.Empty,
                string.Empty,
                TimeSpan.Zero,
                new List<CargoOrderData>()));
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        Setup();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (CargoMap == null)
        {
            DebugTools.Assert(_shuttles.Count == 0);
            return;
        }

        _mapManager.DeleteMap(CargoMap.Value);
        CargoMap = null;

        // Shuttle may not have been in the cargo dimension (e.g. on the station map) so need to delete.
        foreach (var (_, (uid, _)) in _shuttles)
        {
            Del(uid);
        }

        _shuttles.Clear();
    }

    private void Setup()
    {
        if (CargoMap != null)
        {
            _sawmill.Error($"Tried to setup cargo dimension when it's already setup!");
            return;
        }

        // It gets mapinit which is okay... buuutt we still want it paused to avoid power draining.
        CargoMap = _mapManager.CreateMap();
        _mapManager.SetMapPaused(CargoMap!.Value, true);
        var index = 0;

        foreach (var proto in _protoMan.EnumeratePrototypes<CargoShuttlePrototype>())
        {
            var (_, gridId) = _loader.LoadBlueprint(CargoMap.Value, proto.Path.ToString());
            var uid = _mapManager.GetGridEuid(gridId!.Value);
            var xform = Transform(uid);

            // TODO: Something better like a bounds check.
            xform.LocalPosition += 100 * index;
            _shuttles.Add(uid, xform.Coordinates);
            index++;
        }
    }

    #endregion

    public int GetCargoSlots(EntityUid uid)
    {
        // TODO: Need to get the uhh cargo pad idea thing.
        return 0;
    }

    private int GetPrice(EntityUid uid, int price = 0)
    {
        // TODO: Use cargo pads.
        var xform = Transform(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            price += GetPrice(child.Value);
        }

        return price;
    }

    public void SendToCargoDimension(EntityUid uid)
    {
        if (!_shuttles.TryGetValue(uid, out var coordinates))
        {
            _sawmill.Error($"Tried to send non-shuttle entity {ToPrettyString(uid)} to the cargo dimension?");
            DebugTools.Assert(false);
            return;
        }

        Transform(uid).Coordinates = coordinates;
        DebugTools.Assert(MetaData(uid).EntityPaused);

        var newPrice = GetPrice(uid);
        var diff = newPrice - _activeShuttlePrices[uid];
        _activeShuttlePrices.Remove(uid);
    }

    /// <summary>
    /// Retrieves a shuttle for delivery.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="gridUid"></param>
    public void RetrieveFromCargoDimension(EntityUid uid, EntityUid gridUid)
    {
        if (!_shuttles.TryGetValue(uid, out var coordinates))
        {
            _sawmill.Error($"Tried to send non-shuttle entity {ToPrettyString(uid)} to the cargo dimension?");
            DebugTools.Assert(false);
            return;
        }

        Transform(uid).Coordinates = coordinates;
        DebugTools.Assert(MetaData(uid).EntityPaused);
        var price = GetPrice(uid);
        _activeShuttlePrices[uid] = price;
    }

    public bool CanSendToCargoDimension(EntityUid uid)
    {
        return true;
    }
}

[Prototype("cargoShuttle")]
public sealed class CargoShuttlePrototype : IPrototype
{
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [ViewVariables, DataField("path")]
    public ResourcePath Path = default!;
}
