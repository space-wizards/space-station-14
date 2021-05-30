#nullable enable
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Pulling
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPullableComponent))]
    public class PullableComponent : SharedPullableComponent
    {
        [Verb]
        public class PullingVerb : Verb<PullableComponent>
        {
            protected override void GetData(IEntity user, PullableComponent component, VerbData data)
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
                    !user.TryGetComponent(out IPhysBody? userPhysics) ||
                    !component.Owner.TryGetComponent(out IPhysBody? targetPhysics) ||
                    targetPhysics.BodyType == BodyType.Static)
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = component.Puller == userPhysics.Owner
                    ? Loc.GetString("Stop pulling")
                    : Loc.GetString("Pull");
            }

            protected override void Activate(IEntity user, PullableComponent component)
            {
                // There used to be sanity checks here for no reason.
                // Why no reason? Because they're supposed to be performed in TryStartPull.
                if (component.Puller == user)
                {
                    component.TryStopPull();
                }
                else
                {
                    component.TryStartPull(user);
                }
            }
        }
    }
}
