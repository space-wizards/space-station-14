using Content.Shared.SubFloor;
using Robust.Client.GameObjects;

namespace Content.Client.SubFloor;

public sealed class SubFloorHideSystem : SharedSubFloorHideSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

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

        args.Component.TryGetData(SubFloorVisuals.Covered, out bool covered);
        args.Component.TryGetData(SubFloorVisuals.ScannerRevealed, out bool scannerRevealed);

        scannerRevealed &= !ShowAll; // no transparency for show-subfloor mode.

        var revealed = !covered || ShowAll || scannerRevealed;
        var transparency = scannerRevealed ? component.ScannerTransparency : 1f;

        // set visibility & color of each layer
        foreach (var layer in args.Sprite.AllLayers)
        {
            // pipe connection visuals are updated AFTER this, and may re-hide some layers
            layer.Visible = revealed; 

            if (layer.Visible)
                layer.Color = layer.Color.WithAlpha(transparency);
        }

        // Is there some layer that is always visible?
        if (args.Sprite.LayerMapTryGet(SubfloorLayers.FirstLayer, out var firstLayer))
        {
            var layer = args.Sprite[firstLayer];
            layer.Visible = true;
            layer.Color = layer.Color.WithAlpha(1f);
            args.Sprite.Visible = true;
            return;
        }

        args.Sprite.Visible = revealed;
    }

    private void UpdateAll()
    {
        foreach (var (_, appearance) in EntityManager.EntityQuery<SubFloorHideComponent, AppearanceComponent>(true))
        {
            _appearanceSystem.MarkDirty(appearance);
        }
    }
}

public enum SubfloorLayers : byte
{
    FirstLayer, // always visible. E.g. vent part of a vent..
}
