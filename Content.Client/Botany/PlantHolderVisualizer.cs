using Content.Shared.Botany;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.Botany
{
    [UsedImplicitly]
    public sealed class PlantHolderVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapReserveBlank(PlantHolderLayers.Plant);
            sprite.LayerMapReserveBlank(PlantHolderLayers.HealthLight);
            sprite.LayerMapReserveBlank(PlantHolderLayers.WaterLight);
            sprite.LayerMapReserveBlank(PlantHolderLayers.NutritionLight);
            sprite.LayerMapReserveBlank(PlantHolderLayers.AlertLight);
            sprite.LayerMapReserveBlank(PlantHolderLayers.HarvestLight);

            var hydroTools = new ResourcePath("Structures/Hydroponics/overlays.rsi");

            sprite.LayerSetSprite(PlantHolderLayers.HealthLight,
                new SpriteSpecifier.Rsi(hydroTools, "lowhealth3"));
            sprite.LayerSetSprite(PlantHolderLayers.WaterLight,
                new SpriteSpecifier.Rsi(hydroTools, "lowwater3"));
            sprite.LayerSetSprite(PlantHolderLayers.NutritionLight,
                new SpriteSpecifier.Rsi(hydroTools, "lownutri3"));
            sprite.LayerSetSprite(PlantHolderLayers.AlertLight,
                new SpriteSpecifier.Rsi(hydroTools, "alert3"));
            sprite.LayerSetSprite(PlantHolderLayers.HarvestLight,
                new SpriteSpecifier.Rsi(hydroTools, "harvest3"));

            // Let's make those invisible for now.
            sprite.LayerSetVisible(PlantHolderLayers.Plant, false);
            sprite.LayerSetVisible(PlantHolderLayers.HealthLight, false);
            sprite.LayerSetVisible(PlantHolderLayers.WaterLight, false);
            sprite.LayerSetVisible(PlantHolderLayers.NutritionLight, false);
            sprite.LayerSetVisible(PlantHolderLayers.AlertLight, false);
            sprite.LayerSetVisible(PlantHolderLayers.HarvestLight, false);

            // Pretty unshaded lights!
            sprite.LayerSetShader(PlantHolderLayers.HealthLight, "unshaded");
            sprite.LayerSetShader(PlantHolderLayers.WaterLight, "unshaded");
            sprite.LayerSetShader(PlantHolderLayers.NutritionLight, "unshaded");
            sprite.LayerSetShader(PlantHolderLayers.AlertLight, "unshaded");
            sprite.LayerSetShader(PlantHolderLayers.HarvestLight, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

            if (component.TryGetData<string>(PlantHolderVisuals.PlantRsi, out var rsi)
                && component.TryGetData<string>(PlantHolderVisuals.PlantState, out var state))
            {
                var valid = !string.IsNullOrWhiteSpace(state);

                sprite.LayerSetVisible(PlantHolderLayers.Plant, valid);

                if(valid)
                {
                    sprite.LayerSetRSI(PlantHolderLayers.Plant, rsi);
                    sprite.LayerSetState(PlantHolderLayers.Plant, state);
                }
            }

            if (component.TryGetData<bool>(PlantHolderVisuals.HealthLight, out var health))
            {
                sprite.LayerSetVisible(PlantHolderLayers.HealthLight, health);
            }

            if (component.TryGetData<bool>(PlantHolderVisuals.WaterLight, out var water))
            {
                sprite.LayerSetVisible(PlantHolderLayers.WaterLight, water);
            }

            if (component.TryGetData<bool>(PlantHolderVisuals.NutritionLight, out var nutrition))
            {
                sprite.LayerSetVisible(PlantHolderLayers.NutritionLight, nutrition);
            }

            if (component.TryGetData<bool>(PlantHolderVisuals.AlertLight, out var alert))
            {
                sprite.LayerSetVisible(PlantHolderLayers.AlertLight, alert);
            }

            if (component.TryGetData<bool>(PlantHolderVisuals.HarvestLight, out var harvest))
            {
                sprite.LayerSetVisible(PlantHolderLayers.HarvestLight, harvest);
            }
        }
    }

    public enum PlantHolderLayers : byte
    {
        Plant,
        HealthLight,
        WaterLight,
        NutritionLight,
        AlertLight,
        HarvestLight,
    }
}
