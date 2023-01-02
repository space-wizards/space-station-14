using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed class RevenantSystem : VisualizerSystem<RevenantComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RevenantComponent component,
            ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, RevenantVisuals.Harvesting, out bool harvesting) && harvesting)
        {
            args.Sprite.LayerSetState(0, component.HarvestingState);
        }
        else if (AppearanceSystem.TryGetData(uid, RevenantVisuals.Stunned, out bool stunned) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (AppearanceSystem.TryGetData(uid, RevenantVisuals.Corporeal, out bool corporeal))
        {
            if (corporeal)
                args.Sprite.LayerSetState(0, component.CorporealState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
