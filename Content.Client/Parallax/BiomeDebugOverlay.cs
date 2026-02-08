using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Content.Client.Stylesheets.Fonts;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Parallax;

public sealed class BiomeDebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IFontSelectionManager _fontSelection = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    private BiomeSystem _biomes;
    private SharedMapSystem _maps;

    private Font _font;

    public BiomeDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _biomes = _entManager.System<BiomeSystem>();
        _maps = _entManager.System<SharedMapSystem>();

        UpdateFont();
        _fontSelection.OnFontChanged += OnFontChanged;
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _fontSelection.OnFontChanged -= OnFontChanged;
    }

    private void OnFontChanged(StandardFontType type)
    {
        if (type == StandardFontType.Main)
            UpdateFont();
    }

    [MemberNotNull(nameof(_font))]
    private void UpdateFont()
    {
        _font = _fontSelection.GetFont(StandardFontType.Main, 12);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var mapUid = _maps.GetMapOrInvalid(args.MapId);

        return _entManager.HasComponent<BiomeComponent>(mapUid);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.ScreenToMap(mouseScreenPos);

        if (mousePos.MapId == MapId.Nullspace || mousePos.MapId != args.MapId)
            return;

        var mapUid = _maps.GetMapOrInvalid(args.MapId);

        if (!_entManager.TryGetComponent(mapUid, out BiomeComponent? biomeComp) || !_entManager.TryGetComponent(mapUid, out MapGridComponent? grid))
            return;

        var sb = new StringBuilder();
        var nodePos = _maps.WorldToTile(mapUid, grid, mousePos.Position);

        if (_biomes.TryGetEntity(nodePos, biomeComp, (mapUid, grid), out var ent))
        {
            var text = $"Entity: {ent}";
            sb.AppendLine(text);
        }

        if (_biomes.TryGetDecals(nodePos, biomeComp.Layers, biomeComp.Seed, (mapUid, grid), out var decals))
        {
            var text = $"Decals: {decals.Count}";
            sb.AppendLine(text);

            foreach (var decal in decals)
            {
                var decalText = $"- {decal.ID}";
                sb.AppendLine(decalText);
            }
        }

        if (_biomes.TryGetBiomeTile(nodePos, biomeComp.Layers, biomeComp.Seed, (mapUid, grid), out var tile))
        {
            var tileText = $"Tile: {_tileDefManager[tile.Value.TypeId].ID}";
            sb.AppendLine(tileText);
        }

        args.ScreenHandle.DrawString(_font, mouseScreenPos.Position + new Vector2(0f, 32f), sb.ToString());
    }
}
