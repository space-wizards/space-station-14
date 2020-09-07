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

                var dist = user.Transform.GridPosition.Position - component.Owner.Transform.GridPosition.Position;
                if (dist.LengthSquared > SharedInteractionSystem.InteractionRangeSquared)
                {
                    return;
                }

                if (!user.HasComponent<ISharedHandsComponent>() ||
                    !user.TryGetComponent(out ICollidableComponent userCollidable) ||
                    !component.Owner.TryGetComponent(out ICollidableComponent targetCollidable))
                {
                    return;
                }

                var controller = targetCollidable.EnsureController<PullController>();

                data.Visibility = VerbVisibility.Visible;
                data.Text = controller.Puller == userCollidable
                    ? Loc.GetString("Stop pulling")
                    : Loc.GetString("Pull");
            }

            protected override void Activate(IEntity user, SharedPullableComponent component)
            {
                if (!user.TryGetComponent(out ICollidableComponent userCollidable) ||
                    !component.Owner.TryGetComponent(out ICollidableComponent targetCollidable) ||
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
