using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Physics.Pull;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Shared.GameObjects.Components.Pulling
{
    public abstract class SharedPullableComponent : Component
    {
        public override string Name => "Pullable";

        [Verb]
        public class PullingVerb : Verb<SharedPullableComponent>
        {
            protected override void GetData(IEntity user, SharedPullableComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (user == component.Owner)
                {
                    return;
                }

                if (!user.Transform.Coordinates.TryDistance(user.EntityManager, component.Owner.Transform.Coordinates, out var distance) ||
                    distance > SharedInteractionSystem.InteractionRange)
                {
                    return;
                }

                if (!user.HasComponent<ISharedHandsComponent>() ||
                    !user.TryGetComponent(out IPhysicsComponent userPhysics) ||
                    !component.Owner.TryGetComponent(out IPhysicsComponent targetPhysics) ||
                    targetPhysics.Anchored)
                {
                    return;
                }

                var controller = targetPhysics.EnsureController<PullController>();

                data.Visibility = VerbVisibility.Visible;
                data.Text = controller.Puller == userPhysics
                    ? Loc.GetString("Stop pulling")
                    : Loc.GetString("Pull");
            }

            protected override void Activate(IEntity user, SharedPullableComponent component)
            {
                if (!user.TryGetComponent(out IPhysicsComponent userCollidable) ||
                    !component.Owner.TryGetComponent(out IPhysicsComponent targetCollidable) ||
                    targetCollidable.Anchored ||
                    !user.TryGetComponent(out ISharedHandsComponent hands))
                {
                    return;
                }

                var controller = targetCollidable.EnsureController<PullController>();

                if (controller.Puller == userCollidable)
                {
                    hands.StopPull();
                }
                else
                {
                    hands.StartPull(component);
                }
            }
        }
    }
}
