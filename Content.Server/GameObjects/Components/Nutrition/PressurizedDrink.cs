using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class PressurizedDrinkComponent : DrinkComponent, ILand
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override string Name => "PressurizedDrink";

        private string _burstSound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _burstSound, "burstSound", "/Audio/Effects/flash_bang.ogg");
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (_random.Prob(0.25f) &&
                Owner.TryGetComponent(out SolutionComponent component))
            {
                Opened = true;

                var solution = component.SplitSolution(component.CurrentVolume);
                SpillHelper.SpillAt(Owner, solution, "PuddleSmear");

                EntitySystem.Get<AudioSystem>().PlayFromEntity(_burstSound, Owner,
                    AudioParams.Default.WithVolume(-4));
            }
        }
    }
}
