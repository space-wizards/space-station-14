using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Administration.UI.SpawnExplosion;

[UsedImplicitly]
public sealed class SpawnExplosionEui : BaseEui
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private readonly SpawnExplosionWindow _window;
    private ExplosionDebugOverlay? _debugOverlay;

    public SpawnExplosionEui()
    {
        IoCManager.InjectDependencies(this);
        _window = new SpawnExplosionWindow(this);
        _window.OnClose += SendClosedMessage;
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.OnClose -= SendClosedMessage;
        _window.Close();
        ClearOverlay();
    }

    public void SendClosedMessage()
    {
        SendMessage(new CloseEuiMessage());
    }

    public void ClearOverlay()
    {
        if (_overlayManager.HasOverlay<ExplosionDebugOverlay>())
            _overlayManager.RemoveOverlay<ExplosionDebugOverlay>();
        _debugOverlay = null;
    }

    public void RequestPreviewData(MapCoordinates epicenter, string typeId, float totalIntensity, float intensitySlope, float maxIntensity)
    {
        var msg = new SpawnExplosionEuiMsg.PreviewRequest(epicenter, typeId, totalIntensity, intensitySlope, maxIntensity);
        SendMessage(msg);
    }

    /// <summary>
    ///     Receive explosion preview data and add a client-side explosion preview overlay
    /// </summary>
    /// <param name="msg"></param>
    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not SpawnExplosionEuiMsg.PreviewData data)
            return;

        if (_debugOverlay == null)
        {
            _debugOverlay = new();
            _overlayManager.AddOverlay(_debugOverlay);
        }

        var tiles = new Dictionary<EntityUid, Dictionary<int, List<Vector2i>>>();
        _debugOverlay.Tiles.Clear();

        foreach (var (nent, det) in data.Explosion.Tiles)
        {
            tiles[_entManager.GetEntity(nent)] = det;
        }

        _debugOverlay.Tiles = tiles;
        _debugOverlay.SpaceTiles = data.Explosion.SpaceTiles;
        _debugOverlay.Intensity = data.Explosion.Intensity;
        _debugOverlay.Slope = data.Slope;
        _debugOverlay.TotalIntensity = data.TotalIntensity;
        _debugOverlay.Map = data.Explosion.Epicenter.MapId;
        _debugOverlay.SpaceMatrix = data.Explosion.SpaceMatrix;
        _debugOverlay.SpaceTileSize = data.Explosion.SpaceTileSize;
    }
}
