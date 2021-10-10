using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    public class SpillableComponent : Component, IDropped
    {
        public override string Name => "Spillable";

        [DataField("solution")]
        public string SolutionName = "puddle";

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (!eventArgs.Intentional
                && EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solutionComponent))
            {
                EntitySystem.Get<SolutionContainerSystem>()
                    .Drain(Owner.Uid, solutionComponent, solutionComponent.DrainAvailable)
                    .SpillAt(Owner.Transform.Coordinates, "PuddleSmear");
            }
        }
    }
}
