using System.Numerics;
using Content.Server.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server.Tabletop
{
    public sealed partial class TabletopSystem
    {
        /// <summary>
        ///     Ensures that a <see cref="TabletopSession"/> exists on a <see cref="TabletopGameComponent"/>.
        ///     Creates it and sets it up if it doesn't.
        /// </summary>
        /// <param name="tabletop">The tabletop game in question.</param>
        /// <returns>The session for the given tabletop game.</returns>
        public TabletopSession EnsureSession(TabletopGameComponent tabletop)
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

        /// <summary>
        ///     Cleans up a tabletop game session, deleting every entity in it.
        /// </summary>
        /// <param name="uid">The UID of the tabletop game entity.</param>
        public void CleanupSession(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out TabletopGameComponent? tabletop))
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

        /// <summary>
        ///     Adds a player to a tabletop game session, sending a message so the tabletop window opens on their end.
        /// </summary>
        /// <param name="player">The player session in question.</param>
        /// <param name="uid">The UID of the tabletop game entity.</param>
        public void OpenSessionFor(IPlayerSession player, EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out TabletopGameComponent? tabletop) || player.AttachedEntity is not {Valid: true} attachedEntity)
                return;

            // Make sure we have a session, and add the player to it if not added already.
            var session = EnsureSession(tabletop);

            if (session.Players.ContainsKey(player))
                return;

            if(EntityManager.TryGetComponent(attachedEntity, out TabletopGamerComponent? gamer))
                CloseSessionFor(player, gamer.Tabletop, false);

            // Set the entity as an absolute GAMER.
            attachedEntity.EnsureComponent<TabletopGamerComponent>().Tabletop = uid;

            // Create a camera for the gamer to use
            var camera = CreateCamera(tabletop, player);

            session.Players[player] = new TabletopSessionPlayerData { Camera = camera };

            // Tell the gamer to open a viewport for the tabletop game
            RaiseNetworkEvent(new TabletopPlayEvent(GetNetEntity(uid), GetNetEntity(camera), Loc.GetString(tabletop.BoardName), tabletop.Size), player.ConnectedClient);
        }

        /// <summary>
        ///     Removes a player from a tabletop game session, and sends them a message so their tabletop window is closed.
        /// </summary>
        /// <param name="player">The player in question.</param>
        /// <param name="uid">The UID of the tabletop game entity.</param>
        /// <param name="removeGamerComponent">Whether to remove the <see cref="TabletopGamerComponent"/> from the player's attached entity.</param>
        public void CloseSessionFor(IPlayerSession player, EntityUid uid, bool removeGamerComponent = true)
        {
            if (!EntityManager.TryGetComponent(uid, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            if (!session.Players.TryGetValue(player, out var data))
                return;

            if(removeGamerComponent && player.AttachedEntity is {} attachedEntity && EntityManager.TryGetComponent(attachedEntity, out TabletopGamerComponent? gamer))
            {
                // We invalidate this to prevent an infinite feedback from removing the component.
                gamer.Tabletop = EntityUid.Invalid;

                // You stop being a gamer.......
                EntityManager.RemoveComponent<TabletopGamerComponent>(attachedEntity);
            }

            session.Players.Remove(player);
            session.Entities.Remove(data.Camera);

            // Deleting the view subscriber automatically cleans up subscriptions, no need to do anything else.
            EntityManager.QueueDeleteEntity(data.Camera);
        }

        /// <summary>
        ///     A helper method that creates a camera for a specified player, in a tabletop game session.
        /// </summary>
        /// <param name="tabletop">The tabletop game component in question.</param>
        /// <param name="player">The player in question.</param>
        /// <param name="offset">An offset from the tabletop position for the camera. Zero by default.</param>
        /// <returns>The UID of the camera entity.</returns>
        private EntityUid CreateCamera(TabletopGameComponent tabletop, IPlayerSession player, Vector2 offset = default)
        {
            DebugTools.AssertNotNull(tabletop.Session);

            var session = tabletop.Session!;

            // Spawn an empty entity at the coordinates
            var camera = EntityManager.SpawnEntity(null, session.Position.Offset(offset));

            // Add an eye component and disable FOV
            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            _eye.SetDrawFov(camera, false, eyeComponent);
            _eye.SetZoom(camera, tabletop.CameraZoom, eyeComponent);

            // Add the user to the view subscribers. If there is no player session, just skip this step
            _viewSubscriberSystem.AddViewSubscriber(camera, player);

            return camera;
        }
    }
}
