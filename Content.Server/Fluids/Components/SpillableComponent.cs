using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.DragDrop;
using Content.Shared.Notification;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    public class SpillableComponent : Component, IDropped
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
                    !component.Owner.TryGetComponent(out ISolutionInteractionsComponent? solutionComponent) ||
                    !solutionComponent.CanDrain)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Spill liquid");
                data.Visibility = solutionComponent.DrainAvailable > ReagentUnit.Zero
                    ? VerbVisibility.Visible
                    : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, SpillableComponent component)
            {
                if (component.Owner.TryGetComponent<ISolutionInteractionsComponent>(out var solutionComponent))
                {
                    if (!solutionComponent.CanDrain)
                    {
                        user.PopupMessage(user,
                            Loc.GetString("You can't pour anything from {0:theName}!", component.Owner));
                    }

                    if (solutionComponent.DrainAvailable <= 0)
                    {
                        user.PopupMessage(user, Loc.GetString("{0:theName} is empty!", component.Owner));
                    }

                    // Need this as when we split the component's owner may be deleted
                    solutionComponent.Drain(solutionComponent.DrainAvailable).SpillAt(component.Owner.Transform.Coordinates, "PuddleSmear");
                }
            }
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (!eventArgs.Intentional && Owner.TryGetComponent(out ISolutionInteractionsComponent? solutionComponent))
            {
                solutionComponent.Drain(solutionComponent.DrainAvailable).SpillAt(Owner.Transform.Coordinates, "PuddleSmear");
            }
        }
    }
}
