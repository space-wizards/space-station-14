using System;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Chemistry
{
    internal class SolutionComponent : Shared.GameObjects.Components.Chemistry.SolutionComponent
    {
        [Verb]
        private sealed class FillVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if(!user.TryGetComponent<HandsComponent>(out var hands))
                    return string.Empty;

                if(hands.GetActiveHand == null)
                    return string.Empty;

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Fill [{myName}] with [{heldEntityName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    if (hands.GetActiveHand != null)
                    {
                        if (hands.GetActiveHand.Owner.HasComponent<SolutionComponent>())
                            return VerbVisibility.Visible;
                    }
                }

                return VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                throw new NotImplementedException();
            }
        }

        [Verb]
        private sealed class EmptyVerb : Verb<SolutionComponent>
        {
            protected override string GetText(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return string.Empty;

                if (hands.GetActiveHand == null)
                    return string.Empty;

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                return $"Empty [{myName}] into [{heldEntityName}].";
            }

            protected override VerbVisibility GetVisibility(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return VerbVisibility.Disabled;

                if (hands.GetActiveHand == null)
                    return VerbVisibility.Disabled;

                return hands.GetActiveHand.Owner.HasComponent<SolutionComponent>() ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, SolutionComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands))
                    return;

                if (hands.GetActiveHand == null)
                    return;

                var heldEntity = hands.GetActiveHand.Owner;


            }
        }
    }
}
