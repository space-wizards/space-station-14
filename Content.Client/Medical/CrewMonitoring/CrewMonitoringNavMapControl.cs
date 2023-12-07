using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Client.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedTransformSystem _transformSystem = default!;

    public NetEntity? Focus;
    public Dictionary<NetEntity, string> LocalizedNames = new();

    private readonly Font _font;

    public CrewMonitoringNavMapControl() : base()
    {
        IoCManager.InjectDependencies(this);
        var cache = IoCManager.Resolve<IResourceCache>();

        _transformSystem = _entManager.System<SharedTransformSystem>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 12);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (Focus == null)
            return;

        _entManager.TryGetComponent<TransformComponent>(MapUid, out var xform);

        if (xform == null)
            return;

        foreach ((var netEntity, var (coords, _, _, _)) in TrackedEntities)
        {
            if (netEntity != Focus)
                continue;

            var mapPos = coords.ToMap(_entManager, _transformSystem);

            if (mapPos.MapId == MapId.Nullspace)
                return;

            var position = _transformSystem.GetInvWorldMatrix(xform).Transform(mapPos.Position) - GetOffset();
            position = Scale(new Vector2(position.X, -position.Y));

            if (!LocalizedNames.TryGetValue(netEntity, out var name))
                name = "Unknown";

            var labelOffset = new Vector2(0.5f, 0.5f) * MinimapScale;
            var labelPosition = position + labelOffset;
            var message = name + " [x = " + MathF.Round(coords.X) + ", y = " + MathF.Round(coords.Y) + "]";
            var textDimensions = handle.GetDimensions(_font, message, 1f);
            var rectBuffer = new Vector2(5f, 3f);

            handle.DrawRect(new UIBox2(labelPosition, labelPosition + textDimensions + rectBuffer * 2), new Color(128, 128, 128, 20));
            handle.DrawString(_font, labelPosition + rectBuffer, message, Color.White);

            break;
        }
    }
}
