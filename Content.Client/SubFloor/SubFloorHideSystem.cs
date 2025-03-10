using Content.Shared.DrawDepth;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;

namespace Content.Client.SubFloor;

public sealed class SubFloorHideSystem : SharedSubFloorHideSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private bool _showAll;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ShowAll
    {
        get => _showAll;
        set
        {
            if (_showAll == value) return;
            _showAll = value;

            UpdateAll();
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubFloorHideComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, SubFloorHideComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData<bool>(uid, SubFloorVisuals.Covered, out var covered, args.Component);
        _appearance.TryGetData<bool>(uid, SubFloorVisuals.ScannerRevealed, out var scannerRevealed, args.Component);

        scannerRevealed &= !ShowAll; // no transparency for show-subfloor mode.

        var revealed = !covered || ShowAll || scannerRevealed;

        // set visibility & color of each layer
        foreach (var layer in args.Sprite.AllLayers)
        {
            // pipe connection visuals are updated AFTER this, and may re-hide some layers
            layer.Visible = revealed;
        }

        // Is there some layer that is always visible?
        var hasVisibleLayer = false;
        foreach (var layerKey in component.VisibleLayers)
        {
            if (!args.Sprite.LayerMapTryGet(layerKey, out var layerIndex))
                continue;

            var layer = args.Sprite[layerIndex];
            layer.Visible = true;
            layer.Color = layer.Color.WithAlpha(1f);
            hasVisibleLayer = true;
        }

        args.Sprite.Visible = hasVisibleLayer || revealed;

        if (ShowAll)
        {
            // Allows sandbox mode to make wires visible over other stuff.
            component.OriginalDrawDepth ??= args.Sprite.DrawDepth;
            args.Sprite.DrawDepth = (int)Shared.DrawDepth.DrawDepth.Overdoors;
        }
        else if (scannerRevealed)
        {
            // Allows a t-ray to show wires/pipes above carpets/puddles.
            if (component.OriginalDrawDepth is not null)
                return;
            component.OriginalDrawDepth = args.Sprite.DrawDepth;
            var drawDepthDifference = Shared.DrawDepth.DrawDepth.ThickPipe - Shared.DrawDepth.DrawDepth.Puddles;
            args.Sprite.DrawDepth -= drawDepthDifference - 1;
        }
        else if (component.OriginalDrawDepth.HasValue)
        {
            args.Sprite.DrawDepth = component.OriginalDrawDepth.Value;
            component.OriginalDrawDepth = null;
        }
    }

    private void UpdateAll()
    {
        var query = AllEntityQuery<SubFloorHideComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance))
        {
            _appearance.QueueUpdate(uid, appearance);
        }
    }
}
