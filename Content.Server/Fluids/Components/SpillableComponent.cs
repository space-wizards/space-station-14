using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    public class SpillableComponent : Component, IDropped
    {
        public override string Name => "Spillable";

        [DataField("solution")]
        public string SolutionName = "puddle";

        /// <summary>
        ///     Transfers solution from the held container to the floor.
        /// </summary>
        [Verb]
        private sealed class SpillTargetVerb : Verb<SpillableComponent>
        {
            protected override void GetData(IEntity user, SpillableComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                    !EntitySystem.Get<SolutionContainerSystem>()
                            .TryGetDrainableSolution(component.Owner.Uid, out var solutionComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("spill-target-verb-get-data-text");
                data.Visibility = solutionComponent.DrainAvailable > ReagentUnit.Zero
                    ? VerbVisibility.Visible
                    : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, SpillableComponent component)
            {
                var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
                if (component.Owner.HasComponent<SolutionContainerManagerComponent>())
                {
                    if (solutionsSys.TryGetDrainableSolution(component.Owner.Uid, out var solutionComponent))
                    {
                        if (solutionComponent.DrainAvailable <= 0)
                        {
                            user.PopupMessage(user,
                                Loc.GetString("spill-target-verb-activate-is-empty-message", ("owner", component.Owner)));
                        }

                        // Need this as when we split the component's owner may be deleted
                        EntitySystem.Get<SolutionContainerSystem>()
                            .Drain(component.Owner.Uid, solutionComponent, solutionComponent.DrainAvailable)
                            .SpillAt(component.Owner.Transform.Coordinates, "PuddleSmear");
                    }
                    else
                    {
                        user.PopupMessage(user,
                            Loc.GetString("spill-target-verb-activate-cannot-drain-message",
                                ("owner", component.Owner)));
                    }
                }
            }
        }

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
