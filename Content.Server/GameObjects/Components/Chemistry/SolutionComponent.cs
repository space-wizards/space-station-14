using System;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Shared ECS component that manages a liquid solution of reagents.
    /// </summary>
    internal class SolutionComponent : Shared.GameObjects.Components.Chemistry.SolutionComponent
    {
        /// <summary>
        ///     Transfers solution from the held container to the target container.
        /// </summary>
        [Verb]
        private sealed class FillTargetVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if(!user.TryGetComponent<HandsComponent>(out var hands))
                    return "<I SHOULD BE INVISIBLE>";

                if(hands.GetActiveHand == null)
                    return "<I SHOULD BE INVISIBLE>";

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Transfer liquid from [{heldEntityName}] to [{myName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    if (hands.GetActiveHand != null)
                    {
                        if (hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var solution))
                        {
                            if ((solution.Capabilities & SolutionCaps.PourOut) != 0 && (component.Capabilities & SolutionCaps.PourIn) != 0)
                                return VerbVisibility.Visible;
                        }
                    }
                }

                return VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return;

                if (hands.GetActiveHand == null)
                    return;

                if (!hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var handSolutionComp))
                    return;

                if ((handSolutionComp.Capabilities & SolutionCaps.PourOut) == 0 || (component.Capabilities & SolutionCaps.PourIn) == 0)
                    return;

                var transferQuantity = Math.Min(component.MaxVolume - component.CurrentVolume, handSolutionComp.CurrentVolume);
                transferQuantity = Math.Min(transferQuantity, 10);

                // nothing to transfer
                if (transferQuantity <= 0)
                    return;

                var transferSolution = handSolutionComp.SplitSolution(transferQuantity);
                component.TryAddSolution(transferSolution);

            }
        }

        /// <summary>
        ///     Transfers solution from a target container to the held container.
        /// </summary>
        [Verb]
        private sealed class EmptyTargetVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return "<I SHOULD BE INVISIBLE>";

                if (hands.GetActiveHand == null)
                    return "<I SHOULD BE INVISIBLE>";

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Transfer liquid from [{myName}] to [{heldEntityName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    if (hands.GetActiveHand != null)
                    {
                        if (hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var solution))
                        {
                            if ((solution.Capabilities & SolutionCaps.PourIn) != 0 && (component.Capabilities & SolutionCaps.PourOut) != 0)
                                return VerbVisibility.Visible;
                        }
                    }
                }

                return VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return;

                if (hands.GetActiveHand == null)
                    return;

                if(!hands.GetActiveHand.Owner.TryGetComponent<SolutionComponent>(out var handSolutionComp))
                    return;

                if ((handSolutionComp.Capabilities & SolutionCaps.PourIn) == 0 || (component.Capabilities & SolutionCaps.PourOut) == 0)
                    return;

                var transferQuantity = Math.Min(handSolutionComp.MaxVolume - handSolutionComp.CurrentVolume, component.CurrentVolume);
                transferQuantity = Math.Min(transferQuantity, 10);

                // pulling from an empty container, pointless to continue
                if (transferQuantity <= 0)
                    return;
                
                var transferSolution = component.SplitSolution(transferQuantity);
                handSolutionComp.TryAddSolution(transferSolution);
            }
        }
    }
}
