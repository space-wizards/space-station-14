using Content.Server.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Tabletop
{
    public partial class TabletopSystem
    {
        private TabletopSession EnsureSession(TabletopGameComponent tabletop)
        {
            // We already have a session, return it
            // TODO: if tables are connected, treat them as a single entity. This can be done by sharing the session.
            if (tabletop.Session != null)
                return tabletop.Session;

            // We make sure that the tabletop map exists before continuing.
            EnsureTabletopMap();

            // Create new session.
            var session = new TabletopSession(TabletopMap, GetNextTabletopPosition());
            tabletop.Session = session;

            // Since this is the first time opening this session, set up the game
            tabletop.Setup.SetupTabletop(session, EntityManager);

            Logger.Info($"Created tabletop session number {tabletop} at position {session.Position}.");

            return session;
        }

        public void CleanupSession(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out TabletopGameComponent? tabletop))
                return;

            if (tabletop.Session is not { } session)
                return;

            foreach (var (player, _) in session.Players)
            {
                CloseSessionFor(player, uid);
            }

            foreach (var euid in session.Entities)
            {
                EntityManager.QueueDeleteEntity(euid);
            }

            tabletop.Session = null;
        }

        public void OpenSessionFor(IPlayerSession player, EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out TabletopGameComponent? tabletop) || player.AttachedEntity is not {} attachedEntity)
                return;

            // Make sure we have a session, and add the player to it if not added already.
            var session = EnsureSession(tabletop);

            if (session.Players.ContainsKey(player))
                return;

            if(attachedEntity.TryGetComponent<TabletopGamerComponent>(out var gamer))
                CloseSessionFor(player, gamer.Tabletop, false);

            // Set the entity as an absolute GAMER.
            attachedEntity.EnsureComponent<TabletopGamerComponent>().Tabletop = uid;

            // Create a camera for the user to use
            var camera = CreateCamera(tabletop, player);

            session.Players[player] = new TabletopSessionPlayerData { Camera = camera };

            // Tell the client to open a viewport for the tabletop game
            RaiseNetworkEvent(new TabletopPlayEvent(uid, camera, Loc.GetString(tabletop.BoardName), tabletop.Size), player.ConnectedClient);
        }

        public void CloseSessionFor(IPlayerSession player, EntityUid uid, bool removeGamerComponent = true)
        {
            if (!ComponentManager.TryGetComponent(uid, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            if (!session.Players.TryGetValue(player, out var data))
                return;

            if(removeGamerComponent && player.AttachedEntity is {} attachedEntity && attachedEntity.TryGetComponent(out TabletopGamerComponent? gamer))
            {
                // We invalidate this to prevent an infinite feedback from removing the component.
                gamer.Tabletop = EntityUid.Invalid;

                // You stop being a gamer.......
                attachedEntity.RemoveComponent<TabletopGamerComponent>();
            }

            session.Players.Remove(player);
            session.Entities.Remove(data.Camera);

            // Deleting the view subscriber automatically cleans up subscriptions, no need to do anything else.
            EntityManager.QueueDeleteEntity(data.Camera);
        }

        private EntityUid CreateCamera(TabletopGameComponent tabletop, IPlayerSession player, Vector2 offset = default)
        {
            DebugTools.AssertNotNull(tabletop.Session);

            var session = tabletop.Session!;

            // Spawn an empty entity at the coordinates
            var camera = EntityManager.SpawnEntity(null, session.Position.Offset(offset));

            // Add an eye component and disable FOV
            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            eyeComponent.DrawFov = false;
            eyeComponent.Zoom = tabletop.CameraZoom;

            // Add the user to the view subscribers. If there is no player session, just skip this step
            _viewSubscriberSystem.AddViewSubscriber(camera.Uid, player);

            return camera.Uid;
        }
    }
}
