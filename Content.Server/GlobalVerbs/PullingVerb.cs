using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Physics.Pull;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    /// <summary>
    ///     Global verb that pulls an entity.
    /// </summary>
    [GlobalVerb]
    public class PullingVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (user == target ||
                !user.HasComponent<IActorComponent>() ||
                !target.HasComponent<PullableComponent>())
            {
                return;
            }

            var dist = user.Transform.Coordinates.Position - target.Transform.Coordinates.Position;
            if (dist.LengthSquared > SharedInteractionSystem.InteractionRangeSquared)
            {
                return;
            }

            if (!user.HasComponent<ISharedHandsComponent>() ||
                !user.TryGetComponent(out ICollidableComponent userCollidable) ||
                !target.TryGetComponent(out ICollidableComponent targetCollidable) ||
                targetCollidable.Anchored)
            {
                return;
            }

            var controller = targetCollidable.EnsureController<PullController>();

            data.Visibility = VerbVisibility.Visible;
            data.Text = controller.Puller == userCollidable
                ? Loc.GetString("Stop pulling")
                : Loc.GetString("Pull");
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out ICollidableComponent userCollidable) ||
                !target.TryGetComponent(out ICollidableComponent targetCollidable) ||
                targetCollidable.Anchored ||
                !target.TryGetComponent(out PullableComponent pullable) ||
                !user.TryGetComponent(out HandsComponent hands))
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
                hands.StartPull(pullable);
            }
        }
    }
}
