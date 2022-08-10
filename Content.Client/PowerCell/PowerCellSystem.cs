using Content.Shared.PowerCell;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.PowerCell;

[UsedImplicitly]
public sealed class PowerCellSystem : SharedPowerCellSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerCellVisualsComponent, AppearanceChangeEvent>(OnPowerCellVisualsChange);
    }

    private void OnPowerCellVisualsChange(EntityUid uid, PowerCellVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null) return;

        if (args.Component.TryGetData(PowerCellVisuals.ChargeLevel, out byte level))
        {
            if (level == 0)
            {
                args.Sprite.LayerSetVisible(PowerCellVisualLayers.Unshaded, false);
                return;
            }

            args.Sprite.LayerSetVisible(PowerCellVisualLayers.Unshaded, true);
            args.Sprite.LayerSetState(PowerCellVisualLayers.Unshaded, $"o{level}");
        }
    }

    private enum PowerCellVisualLayers : byte
    {
        Base,
        Unshaded,
    }
}
