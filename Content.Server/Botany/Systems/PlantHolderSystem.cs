using Content.Server.Botany.Components;
using Content.Shared.Examine;

namespace Content.Server.Botany.Systems
{
    public sealed class PlantHolderSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, PlantHolderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (component.Seed == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!component.Dead)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                                      ("seedName", component.Seed.DisplayName),
                                      ("toBeForm", component.Seed.DisplayName.EndsWith('s') ? "are" : "is")));

                if (component.Health <= component.Seed.Endurance / 2)
                    args.PushMarkup(Loc.GetString(
                                          "plant-holder-component-something-already-growing-low-health-message",
                                          ("healthState",
                                              Loc.GetString(component.Age > component.Seed.Lifespan
                                                  ? "plant-holder-component-plant-old-adjective"
                                                  : "plant-holder-component-plant-unhealthy-adjective"))));
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }

            if (component.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (component.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            args.PushMarkup(Loc.GetString($"plant-holder-component-water-level-message",
                ("waterLevel", (int) component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int) component.NutritionLevel)));

            if (component.DrawWarnings)
            {
                if (component.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (component.ImproperLight)
                    args.PushMarkup(Loc.GetString("plant-holder-component-light-improper-warning"));

                if (component.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (component.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (component.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }
    }
}
