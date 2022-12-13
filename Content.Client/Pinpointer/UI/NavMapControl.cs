using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map.Components;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
public sealed class NavMapControl : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public EntityUid? Uid;

    public const int MinimapRadius = 320;
    private const int MinimapMargin = 4;

    private float _radarMinRange = 64f;
    private float _radarMaxRange = 256f;
    public float RadarRange { get; private set; } = 32f;

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    private float _actualRadarRange = 256f;

    /// <summary>
    /// Controls the maximum distance that IFF labels will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;
    private int MidPoint => SizeFull / 2;
    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => RadarRange != 0 ? ScaledMinimapRadius / RadarRange : 0f;

    public NavMapControl()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_entManager.TryGetComponent<NavMapComponent>(Uid, out var navMap) ||
            !_entManager.TryGetComponent<TransformComponent>(Uid, out var xform) ||
            !_entManager.TryGetComponent<MapGridComponent>(Uid, out var grid))
        {
            return;
        }

        var area = new Box2(-RadarRange, -RadarRange, RadarRange, RadarRange);

        for (var x = Math.Floor(area.Left); x <= Math.Ceiling(area.Right); x += SharedNavMapSystem.ChunkSize * grid.TileSize)
        {
            for (var y = Math.Floor(area.Bottom); y <= Math.Ceiling(area.Top); y += SharedNavMapSystem.ChunkSize * grid.TileSize)
            {
                var floored = new Vector2i((int) x, (int) y);

                var chunkOrigin = SharedMapSystem.GetChunkIndices(floored, SharedNavMapSystem.ChunkSize);

                if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
                    continue;

                // TODO: Okay maybe I should just use ushorts lmao...
                for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                {
                    var value = (int) Math.Pow(2, i);

                    var mask = chunk.TileData & value;

                    if (mask == 0x0)
                        continue;

                    var tile = chunk.Origin * SharedNavMapSystem.ChunkSize + SharedNavMapSystem.GetTile(mask);
                    tile = new Vector2i(tile.X, tile.Y * -1);
                    handle.DrawRect(new UIBox2(Scale(tile * grid.TileSize), Scale((tile + 1) * grid.TileSize)), Color.Aqua, false);
                }
            }
        }
    }

    private Vector2 Scale(Vector2i position)
    {
        return position * MinimapScale + MidPoint;
    }
}
