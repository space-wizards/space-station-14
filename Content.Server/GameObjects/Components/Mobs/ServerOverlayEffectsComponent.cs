using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {
        public ServerOverlayEffectsComponent()
        {
            NetSyncEnabled = false;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public List<OverlayContainer> ActiveOverlays { get; } = new();

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            if (Owner.TryGetComponent(out IActorComponent actor) && message is ResendOverlaysMessage)
            {
                if (actor.playerSession.ConnectedClient == netChannel)
                {
                    SyncClient();
                }
            }
        }

        public void AddOverlay(string id) => AddOverlay(new OverlayContainer(id));

        public void AddOverlay(SharedOverlayID id) => AddOverlay(new OverlayContainer(id));

        public void AddOverlay(OverlayContainer container)
        {
            if (!ActiveOverlays.Contains(container))
            {
                ActiveOverlays.Add(container);
                SyncClient();
            }
        }

        public void RemoveOverlay(SharedOverlayID id) => RemoveOverlay(id.ToString());

        public void RemoveOverlay(string id) => RemoveOverlay(new OverlayContainer(id));

        public void RemoveOverlay(OverlayContainer container)
        {
            if (ActiveOverlays.Remove(container))
            {
                SyncClient();
            }
        }

        public bool TryModifyOverlay(string id, Action<OverlayContainer> modifications)
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

        public void ClearOverlays()
        {
            if (ActiveOverlays.Count == 0)
            {
                return;
            }

            ActiveOverlays.Clear();
            SyncClient();
        }

        private void SyncClient()
        {
            if (Owner.TryGetComponent(out IActorComponent actor))
            {
                if (actor.playerSession.ConnectedClient.IsConnected)
                {
                    SendNetworkMessage(new OverlayEffectComponentMessage(ActiveOverlays), actor.playerSession.ConnectedClient);
                }
            }
        }
    }
}
