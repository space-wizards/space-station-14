using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.MapText;

/// <summary>
/// This handles registering the map text overlay
/// </summary>
public sealed class MapTextSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private MapTextOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _overlay = new MapTextOverlay(_configManager, _entManager, _uiManager, _transform, _resourceCache, _prototypeManager);
        _overlayManager.AddOverlay(_overlay);
    }
}
