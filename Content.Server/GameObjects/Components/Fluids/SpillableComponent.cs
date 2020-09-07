using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    public class SpillableComponent : Component
    {
        public override string Name => "Spillable";

        /// <summary>
        ///     Transfers solution from the held container to the floor.
        /// </summary>
        [Verb]
        private sealed class SpillTargetVerb : Verb<SpillableComponent>
        {
            protected override void GetData(IEntity user, SpillableComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !component.Owner.TryGetComponent(out SolutionContainerComponent solutionComponent) ||
                    !solutionComponent.CanRemoveSolutions)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Spill liquid");
                data.Visibility = solutionComponent.CurrentVolume > ReagentUnit.Zero ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, SpillableComponent component)
            {
                if (component.Owner.TryGetComponent<SolutionContainerComponent>(out var solutionComponent))
                {
                    if (!solutionComponent.CanRemoveSolutions)
                    {
                        user.PopupMessage(user, Loc.GetString("You can't pour anything from {0:theName}!", component.Owner));
                    }

                    if (solutionComponent.CurrentVolume.Float() <= 0)
                    {
                        user.PopupMessage(user, Loc.GetString("{0:theName} is empty!", component.Owner));
                    }

                    // Need this as when we split the component's owner may be deleted
                    var entityLocation = component.Owner.Transform.Coordinates;
                    var solution = solutionComponent.SplitSolution(solutionComponent.CurrentVolume);
                    solution.SpillAt(entityLocation, "PuddleSmear");
                }
            }
        }
    }
}
