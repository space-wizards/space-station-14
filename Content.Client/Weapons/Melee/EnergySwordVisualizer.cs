using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Weapons.Melee;

public class EnergySwordVisualizer : AppearanceVisualizer
{
    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);
        var entManager = IoCManager.Resolve<IEntityManager>();

        component.TryGetData(EnergySwordVisuals.State, out EnergySwordStatus? status);
        component.TryGetData(EnergySwordVisuals.Color, out Color? color);
        color ??= Color.DodgerBlue;
        entManager.TryGetComponent(component.Owner, out SpriteComponent? spriteComponent);

        switch (status)
        {
            case EnergySwordStatus.On:
                TurnOn(component, color.Value, entManager, spriteComponent);
                break;
            default:
                TurnOff(component, entManager, spriteComponent);
                break;
        }
    }

    private void TurnOn(
        AppearanceComponent component,
        Color color,
        IEntityManager entManager,
        SpriteComponent? spriteComponent = null)
    {
        if (component.TryGetData(EnergySwordVisuals.Hacked, out bool hacked) && hacked)
        {
            if (entManager.TryGetComponent(component.Owner, out SharedItemComponent? itemComponent))
            {
                itemComponent.EquippedPrefix = "on-rainbow";
            }

            //todo: figure out how to use the RGBLightControllerSystem to phase out the rainbow sprite AND add lights.
            spriteComponent?.LayerSetColor(1, Color.White);
            spriteComponent?.LayerSetVisible(1, false);
            spriteComponent?.LayerSetState(0, "e_sword_rainbow_on");
        }
        else
        {
            if (entManager.TryGetComponent(component.Owner, out SharedItemComponent? itemComponent))
            {
                itemComponent.EquippedPrefix = "on";
                itemComponent.Color = color;
            }

            spriteComponent?.LayerSetColor(1, color);
            spriteComponent?.LayerSetVisible(1, true);

            if (entManager.TryGetComponent(component.Owner, out PointLightComponent? pointLightComponent))
            {
                pointLightComponent.Color = color;
                pointLightComponent.Enabled = true;
            }
        }
    }

    private void TurnOff(
        AppearanceComponent component,
        IEntityManager entManager,
        SpriteComponent? spriteComponent = null)
    {
        if (entManager.TryGetComponent(component.Owner, out SharedItemComponent? itemComponent))
        {
            itemComponent.EquippedPrefix = "off";
        }

        if (component.TryGetData(EnergySwordVisuals.Hacked, out bool hacked) && hacked)
        {
            spriteComponent?.LayerSetState(0, "e_sword");
        }
        else
        {
            spriteComponent?.LayerSetVisible(1, false);
        }

        if (entManager.TryGetComponent(component.Owner, out PointLightComponent? pointLightComponent))
        {
            pointLightComponent.Enabled = false;
        }
    }
}
