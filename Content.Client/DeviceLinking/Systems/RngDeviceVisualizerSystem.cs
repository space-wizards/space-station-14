using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Visuals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.DeviceLinking.Visuals.RngDeviceVisuals;

namespace Content.Client.DeviceLinking.Systems;

public sealed class RngDeviceVisualizerSystem : VisualizerSystem<RngDeviceVisualsComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, RngDeviceVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_appearance.TryGetData<string>(uid, State, out var stateVal, args.Component))
        {
            sprite.LayerSetState("dice", stateVal);
        }
    }
}
