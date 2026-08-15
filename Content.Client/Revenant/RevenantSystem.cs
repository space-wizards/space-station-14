using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed partial class RevenantSystem : VisualizerSystem<RevenantComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, RevenantComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.TryGetData<bool>(RevenantVisuals.Harvesting, out var harvesting) && harvesting)
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, component.HarvestingState);

        else if (args.TryGetData<bool>(RevenantVisuals.Stunned, out var stunned) && stunned)
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, component.StunnedState);

        else if (args.TryGetData<bool>(RevenantVisuals.Corporeal, out var corporeal))
        {
            if (corporeal)
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, component.CorporealState);
            else
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, component.State);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetCounterAmount(Entity<RevenantComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.EssenceAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Essence.Int();
    }
}
