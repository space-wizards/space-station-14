using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.NPC.HTN;

public sealed class HTNOverlay : Overlay
{
    private readonly IEntityManager _entManager = default!;
    private readonly IFontSelectionManager _fontSelection;
    private Font _font = default!;
    private readonly SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public HTNOverlay(IEntityManager entManager, IFontSelectionManager fontSelection)
    {
        _entManager = entManager;
        _fontSelection = fontSelection;
        _transformSystem = _entManager.System<SharedTransformSystem>();

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
        _font = _fontSelection.GetFont(StandardFontType.Main, 10);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var handle = args.ScreenHandle;

        foreach (var (comp, xform) in _entManager.EntityQuery<HTNComponent, TransformComponent>(true))
        {
            if (string.IsNullOrEmpty(comp.DebugText) || xform.MapID != args.MapId)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform);

            if (!args.WorldAABB.Contains(worldPos))
                continue;

            var screenPos = args.ViewportControl.WorldToScreen(worldPos);
            handle.DrawString(_font, screenPos + new Vector2(0, 10f), comp.DebugText, Color.White);
        }
    }
}
