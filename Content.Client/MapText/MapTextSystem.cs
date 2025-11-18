using Content.Shared.MapText;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.MapText;

/// <inheritdoc/>
public sealed class MapTextSystem : SharedMapTextSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private MapTextOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MapTextComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<MapTextComponent, ComponentHandleState>(HandleCompState);

        _overlay = new MapTextOverlay(_configManager, EntityManager, _uiManager, _transform, _resourceCache, _prototypeManager);
        _overlayManager.AddOverlay(_overlay);

        // TODO move font prototype to robust.shared, then use ProtoId<FontPrototype>
        DebugTools.Assert(_prototypeManager.HasIndex<FontPrototype>(SharedMapTextComponent.DefaultFont));
    }

    private void OnComponentStartup(Entity<MapTextComponent> ent, ref ComponentStartup args)
    {
        CacheText(ent.Comp);
        // TODO move font prototype to robust.shared, then use ProtoId<FontPrototype>
        DebugTools.Assert(_prototypeManager.HasIndex<FontPrototype>(ent.Comp.FontId));
    }

    private void HandleCompState(Entity<MapTextComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not MapTextComponentState state)
            return;

        ent.Comp.Text = state.Text;
        ent.Comp.LocText = state.LocText;
        ent.Comp.Color = state.Color;
        ent.Comp.FontId = state.FontId;
        ent.Comp.FontSize = state.FontSize;
        ent.Comp.Offset = state.Offset;

        CacheText(ent.Comp);
    }

    private void CacheText(MapTextComponent component)
    {
        component.CachedFont = null;

        component.CachedText = string.IsNullOrWhiteSpace(component.Text)
            ? Loc.GetString(component.LocText)
            : component.Text;

        if (!_prototypeManager.TryIndex<FontPrototype>(component.FontId, out var fontPrototype))
        {
            component.CachedText = Loc.GetString("map-text-font-error");
            component.Color = Color.Red;

            if(_prototypeManager.TryIndex<FontPrototype>(SharedMapTextComponent.DefaultFont, out var @default))
                component.CachedFont = new VectorFont(_resourceCache.GetResource<FontResource>(@default.Path), 14);
            return;
        }

        var fontResource = _resourceCache.GetResource<FontResource>(fontPrototype.Path);
        component.CachedFont = new VectorFont(fontResource, component.FontSize);
    }
}
