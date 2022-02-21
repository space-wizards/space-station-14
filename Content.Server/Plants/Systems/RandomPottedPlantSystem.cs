using Content.Server.Plants.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Plants.Systems
{
    public sealed class RandomPottedPlantSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private static readonly string[] RegularPlantStates =
        {
            "plant-01", "plant-02", "plant-03", "plant-04", "plant-05",
            "plant-06", "plant-07", "plant-08", "plant-09", "plant-10",
            "plant-11", "plant-12", "plant-13", "plant-14", "plant-15",
            "plant-16", "plant-17", "plant-18", "plant-19", "plant-20",
            "plant-21", "plant-22", "plant-23", "plant-24", "applebush"
        };
        private static readonly string[] PlasticPlantStates =
        {
            "plant-26", "plant-27", "plant-28", "plant-29"
        };

        public override void Initialize()
        {
            SubscribeLocalEvent<RandomPottedPlantComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, RandomPottedPlantComponent component, MapInitEvent args)
        {
            component.State ??= _random.Pick(component.Plastic ? PlasticPlantStates : RegularPlantStates);
            Comp<SpriteComponent>(uid).LayerSetState(0, component.State);
        }
    }
}
