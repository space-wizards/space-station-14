using Content.Shared.ActionBlocker;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Tabletop.Components
{
    /**
     * <summary>A component that makes an object playable as a tabletop game.</summary>
     */
    [RegisterComponent]
    public class TabletopGameComponent : Component
    {
        public override string Name => "TabletopGame";

        /**
         * <summary>A verb that allows the player to start playing a tabletop game.</summary>
         */
        [Verb]
        public class PlayVerb : Verb<TabletopGameComponent>
        {
            protected override void GetData(IEntity user, TabletopGameComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                // TODO: use localisation
                data.Text = "Play Game";
                // TODO: add icon
            }

            protected override void Activate(IEntity user, TabletopGameComponent component)
            {
                // Tell the client that it has to open a viewport for the tabletop game
                var entityNetManager = component.Owner.EntityManager.EntityNetManager;
                // TODO: use actual title/size from prototype
                entityNetManager?.SendSystemNetworkMessage(new TabletopPlayEvent(/*user.Uid,*/CreateCamera(component), "Chess", (400, 400)));
            }

            private static IEntity CreateCamera(TabletopGameComponent component)
            {
                var entityManager = component.Owner.EntityManager;
                var mapManager = IoCManager.Resolve<IMapManager>();

                var eyeCoordinates = EntityCoordinates.FromMap(entityManager, mapManager, new MapCoordinates((0, 0), new MapId(1)));
                var camera = entityManager.SpawnEntity(null, eyeCoordinates);

                camera.EnsureComponent<EyeComponent>();
                camera.EnsureComponent<ActorComponent>();

                return camera;
            }
        }
    }
}
