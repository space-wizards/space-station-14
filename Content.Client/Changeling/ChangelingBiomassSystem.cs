using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Alert.Components;
using Content.Shared.Changeling.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Changeling;

public sealed class ChangelingBiomassSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingBiomassComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
        SubscribeLocalEvent<ChangelingBiomassComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnUpdateAlertSprite(EntityUid uid, ChangelingBiomassComponent component, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert != component.BiomassAlert)
            return;

        var stateNumber = Math.Clamp((int)(component.CurrentBiomass / component.MaxBiomass * component.BiomassLayerStates), 0, component.BiomassLayerStates);

        _sprite.LayerSetRsiState(args.SpriteViewEnt.AsNullable(), AlertVisualLayers.Base, $"bio{stateNumber}");
    }

    private void OnGetCounterAmount(Entity<ChangelingBiomassComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BiomassAlert != args.Alert)
            return;

        args.Amount = (int)ent.Comp.CurrentBiomass;
    }
}
