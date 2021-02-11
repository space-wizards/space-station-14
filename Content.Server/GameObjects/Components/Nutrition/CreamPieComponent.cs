using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class CreamPieComponent : Component, ILand
    {
        public override string Name => "CreamPie";

        public void PlaySound()
        {
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(AudioHelpers.GetRandomFileFromSoundCollection("desecration"), Owner,
                AudioHelpers.WithVariation(0.125f));
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            PlaySound();

            if (Owner.TryGetComponent(out SolutionContainerComponent solution))
            {
                solution.Solution.SpillAt(Owner, "PuddleSmear", false);
            }

            Owner.Delete();
        }
    }
}
