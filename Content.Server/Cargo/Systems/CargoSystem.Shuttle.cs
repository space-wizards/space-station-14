using Content.Server.GameTicking.Events;
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

    private MapId? _cargoDimension;
    private readonly Dictionary<EntityUid, EntityCoordinates> _shuttles = new();

    /// <summary>
    /// Prices for shuttles that are currently departed on the station.
    /// We use it to determine the sell price when they send it back.
    /// </summary>
    private readonly Dictionary<EntityUid, int> _activeShuttlePrices = new();

    #region Setup

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
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
        if (_cargoDimension == null)
        {
            _sawmill.Error($"Tried to tear down cargo dimension when it's already torn down!");
            DebugTools.Assert(_shuttles.Count == 0);
            return;
        }

        _mapManager.DeleteMap(_cargoDimension.Value);
        _cargoDimension = null;

        // Shuttle may not have been in the cargo dimension (e.g. on the station map) so need to delete.
        foreach (var (_, (uid, _)) in _shuttles)
        {
            Del(uid);
        }

        _shuttles.Clear();
    }

    private void Setup()
    {
        if (_cargoDimension != null)
        {
            _sawmill.Error($"Tried to setup cargo dimension when it's already setup!");
            return;
        }

        // It gets mapinit which is okay... buuutt we still want it paused to avoid power draining.
        _cargoDimension = _mapManager.CreateMap();
        _mapManager.SetMapPaused(_cargoDimension!.Value, true);
        var index = 0;

        foreach (var proto in _protoMan.EnumeratePrototypes<CargoShuttlePrototype>())
        {
            var (_, gridId) = _loader.LoadBlueprint(_cargoDimension.Value, proto.Path.ToString());
            var uid = _mapManager.GetGridEuid(gridId!.Value);
            var xform = Transform(uid);

            // TODO: Something better like a bounds check.
            xform.LocalPosition += 100 * index;
            _shuttles.Add(uid, xform.Coordinates);
            index++;
        }
    }

    #endregion

    // TODO: Get price before it leaves and after it leaves

    private int GetPrice(EntityUid uid, int price = 0)
    {
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
