using Content.Client.AME.Components;
using Robust.Client.GameObjects;
using static Content.Shared.AME.SharedAMEShieldComponent;

namespace Content.Client.AME;

public sealed class AmeShieldingVisualizerSystem : VisualizerSystem<AmeShieldingVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AmeShieldingVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, AmeShieldVisuals.Core, out var core, args.Component))
            core = false;

        args.Sprite.LayerSetVisible(AmeShieldingVisualsLayer.Core, core);

        if (!AppearanceSystem.TryGetData<AmeCoreState>(uid, AmeShieldVisuals.CoreState, out var coreState, args.Component))
            coreState = AmeCoreState.Off;

        switch (coreState)
        {
            case AmeCoreState.Weak:
                args.Sprite.LayerSetState(AmeShieldingVisualsLayer.CoreState, component.StableState);
                args.Sprite.LayerSetVisible(AmeShieldingVisualsLayer.CoreState, true);
                break;
            case AmeCoreState.Strong:
                args.Sprite.LayerSetState(AmeShieldingVisualsLayer.CoreState, component.UnstableState);
                args.Sprite.LayerSetVisible(AmeShieldingVisualsLayer.CoreState, true);
                break;
            case AmeCoreState.Off:
                args.Sprite.LayerSetVisible(AmeShieldingVisualsLayer.CoreState, false);
                break;
        }
    }
}

public enum AmeShieldingVisualsLayer : byte
{
    Core,
    CoreState,
}
