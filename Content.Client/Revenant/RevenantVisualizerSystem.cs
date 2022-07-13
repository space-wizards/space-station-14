using Content.Shared.Revenant;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed class RevenantVisualsSystem : VisualizerSystem<RevenantVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RevenantVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(RevenantVisuals.Harvesting, out bool harvesting) && harvesting)
        {
            args.Sprite.LayerSetState(0, component.HarvestingState);
        }
        else if (args.Component.TryGetData(RevenantVisuals.Stunned, out bool stunned) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (args.Component.TryGetData(RevenantVisuals.Corporeal, out bool corporeal))
        {
            if (corporeal)
                args.Sprite.LayerSetState(0, component.CorporealState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
