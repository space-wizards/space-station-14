using Robust.Client.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Content.Client.SS220.ViewableStationMap.UI;
using Content.Shared.SS220.ViewableStationMap;
using JetBrains.Annotations;

namespace Content.Client.SS220.ViewableStationMap;

[UsedImplicitly]
public sealed class StationViewableMapBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private ViewableStationMapWindow? _window;

    public StationViewableMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new ViewableStationMapWindow();
        _window.OpenCentered();

        _window.OnClose += Close;

        if (_entMan.TryGetComponent(Owner, out ViewableStationMapComponent? comp))
        {
            var path = SpriteSpecifierSerializer.TextureRoot / comp.MapTexture;
            _window.ViewedMap = path;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
