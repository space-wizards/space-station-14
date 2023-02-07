using Content.Shared.PDA;
using Content.Shared.Light;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PDASystem : SharedPDASystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PDAComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, PDAComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.Sprite.LayerSetState(PDAVisualLayers.Base, component.State);
        if (_appearance.TryGetData<bool>(uid, UnpoweredFlashlightVisuals.LightOn, out var isFlashlightOn, args.Component))
            args.Sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isFlashlightOn);

        if (_appearance.TryGetData<bool>(uid, PDAVisuals.IDCardInserted, out var isCardInserted, args.Component))
            args.Sprite.LayerSetVisible(PDAVisualLayers.IDLight, isCardInserted);
    }
}

enum PDAVisualLayers : byte
{
    Base,
    Flashlight,
    IDLight
}
