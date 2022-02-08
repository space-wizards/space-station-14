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

    private void OnAppearanceChanged(EntityUid uid, SubFloorHideComponent component, AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!args.Component.TryGetData(SubFloorVisuals.SubFloor, out bool subfloor))
            return;

        subfloor |= ShowAll;

        foreach (var layer in sprite.AllLayers)
        {
            layer.Visible = subfloor;
        }

        if (!sprite.LayerMapTryGet(SubfloorLayers.FirstLayer, out var firstLayer))
        {
            sprite.Visible = subfloor;
            return;
        }

        // show the top part of the sprite. E.g. the grille-part of a vent, but not the connecting pipes.
        sprite.LayerSetVisible(firstLayer, true);
        sprite.Visible = true;
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
