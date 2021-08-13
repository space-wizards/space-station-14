using Content.Server.Fluids.Components;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    public class CreamPieComponent : Component, ILand, IThrowCollide
    {
        public override string Name => "CreamPie";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 1f;

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

            if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "food", out var solution))
            {
                solution.SpillAt(Owner, "PuddleSmear", false);
            }

            Owner.QueueDelete();
        }
    }
}
