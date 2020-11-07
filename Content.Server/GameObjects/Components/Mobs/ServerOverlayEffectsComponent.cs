using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {

        [ViewVariables(VVAccess.ReadWrite)]
        public List<OverlayContainer> ActiveOverlays { get; } = new List<OverlayContainer>();

        public ServerOverlayEffectsComponent()
        {
            NetSyncEnabled = false;
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            switch (message)
            {
                case RequestOverlayEffectsSyncMessage msg:
                    if(Owner.TryGetComponent(out IActorComponent actor) && actor.playerSession.ConnectedClient == netChannel)
                        SyncClient();
                    break;
            }
        }



        /// <summary>
        ///     Creates an <see cref="OverlayContainer"/> and add it to this component, syncing it with the client (if there is one currently connected). Returns the new container.
        /// </summary>
        public Guid AddNewOverlay(OverlayType type, OverlayParameter parameter)
        {
            return AddNewOverlay(type, new OverlayParameter[] { parameter });
        }
        /// <summary>
        ///     Creates an <see cref="OverlayContainer"/> and add it to this component, syncing it with the client (if there is one currently connected). Returns the new container.
        /// </summary>
        public Guid AddNewOverlay(OverlayType type)
        {
            return AddNewOverlay(type, new OverlayParameter[] { });
        }

        /// <summary>
        ///     Creates an <see cref="OverlayContainer"/> and add it to this component, syncing it with the client (if there is one currently connected). Returns the new container.
        /// </summary>
        public Guid AddNewOverlay(OverlayType type, OverlayParameter[] parameters)
        {
            OverlayContainer container = new OverlayContainer(Guid.NewGuid(), type, parameters);
            ActiveOverlays.Add(container);
            SyncClient();
            return container.ID;
        }

        /// <summary>
        ///     Removes the given overlay from this component and syncs it with the client (if possible). Returns whether removal was successful.
        /// </summary>
        public bool TryRemoveOverlay(OverlayContainer container)
        {
            return TryRemoveOverlay(container.ID);
        }
        /// <summary>
        ///     Removes the given overlay from this component and syncs it with the client (if possible). Returns whether removal was successful.
        /// </summary>
        public bool TryRemoveOverlay(Guid id)
        {
            var container = ActiveOverlays.Find(c => c.ID == id);
            if (container != null)
            {
                ActiveOverlays.Remove(container);
                SyncClient();
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets all overlays of a given type. Returns true if one or more was found.
        /// </summary>
        public bool TryGetOverlaysOfType(OverlayType type, out List<Guid> ids)
        {
            ids = new List<Guid>();
            foreach (var overlay in ActiveOverlays)
            {
                if (overlay.OverlayType == type)
                    ids.Add(overlay.ID);
            }
            return ids.Count > 0;
        }

        /// <summary>
        ///     Removes all overlays of a given type.
        /// </summary>
        public void RemoveOverlaysOfType(OverlayType type)
        {
            bool doSync = false;
            foreach (var overlay in ActiveOverlays)
            {
                if (overlay.OverlayType == type)
                {
                    ActiveOverlays.Remove(overlay);
                    doSync = true;
                }
            }
            if(doSync)
                SyncClient();
        }

        public bool ContainsOverlay(Guid id)
        {
            return ActiveOverlays.Exists(c => c.ID == id);
        }




        /// <summary>
        ///     If overlay with the given Guid exists, returns true and allows you to modify it through an <see cref="OverlayContainer"/> action (automatically syncs with client). Note that
        ///     this will update all parameters, so if you really care about efficiency consider using <see cref="TryUpdateOverlay"/> instead to update specific parameters.
        /// </summary>
        public bool TryModifyOverlay(Guid id, Action<OverlayContainer> modifications)
        {
            var overlay = ActiveOverlays.Find(c => c.ID == id);
            if (overlay == null)
            {
                return false;
            }

            modifications(overlay);
            SyncClient();
            return true;
        }

        /// <summary>
        ///     If overlay with the given Guid exists, returns true and applies the parameters both server and client side. 
        /// </summary>
        public bool TryUpdateOverlay(Guid id, OverlayParameter[] parameters)
        {
            var overlay = ActiveOverlays.Find(c => c.ID == id);
            if (overlay == null)
            {
                return false;
            }
            UpdateClient(id, parameters);
            return true;
        }

        /// <summary>
        ///     Removes ALL active overlays from this component.
        /// </summary>
        public void ClearAllOverlays()
        {
            if (ActiveOverlays.Count == 0)
            {
                return;
            }

            ActiveOverlays.Clear();
            SyncClient();
        }



        private bool TryGetOverlay(Guid id, out OverlayContainer container)
        {
            container = ActiveOverlays.Find(c => c.ID == id);
            return container == null;
        }



        private void SyncClient()
        {
            if (Owner.TryGetComponent(out IActorComponent actor) && actor.playerSession.ConnectedClient.IsConnected){
                SendNetworkMessage(new OverlayEffectsSyncMessage(ActiveOverlays), actor.playerSession.ConnectedClient);
            }
        }

        private void UpdateClient(Guid id, OverlayParameter[] parameters)
        {
            if (Owner.TryGetComponent(out IActorComponent actor) && actor.playerSession.ConnectedClient.IsConnected)
            {
                SendNetworkMessage(new OverlayEffectsUpdateMessage(id, parameters), actor.playerSession.ConnectedClient);
            }
        }
    }
}
