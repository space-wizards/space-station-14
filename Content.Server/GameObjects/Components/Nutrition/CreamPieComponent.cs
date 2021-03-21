using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class CreamPieComponent : Component, ILand, IThrowCollide
    {
        public override string Name => "CreamPie";

        public void PlaySound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), AudioHelpers.GetRandomFileFromSoundCollection("desecration"), Owner,
                AudioHelpers.WithVariation(0.125f));
        }

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            Splat();
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            Splat();
        }

        public void Splat()
        {
            PlaySound();

            if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                solution.Solution.SpillAt(Owner, "PuddleSmear", false);
            }

            Owner.Delete();
        }
    }
}
