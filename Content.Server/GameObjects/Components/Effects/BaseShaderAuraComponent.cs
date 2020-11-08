using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Effects
{

    /// <summary>
    ///      MUST BE IMPLEMENTED. If a player is in range, calls the OnEnterRange function with a reference to its <see cref="ServerOverlayEffectsComponent"/>. Similarly, calls
    ///      OnExitRange on exit. Useful for permanent effects, like a singularity warping space around it.
    /// </summary>
    [RegisterComponent]
    public class BaseShaderAuraComponent : Component
    {
        public override string Name => "ShaderAura";

        [Dependency] protected readonly IPlayerManager PlayerManager = default!;
        [Dependency] protected readonly IEntityManager EntityManager = default!;

        protected List<IPlayerSession> ActivatedPlayers = new List<IPlayerSession>();
        protected virtual int Radius => 20;

        public override void OnRemove()
        {
            base.OnRemove();
            foreach (var player in ActivatedPlayers)
            {
                if (player.AttachedEntityUid != null && EntityManager.TryGetEntity((EntityUid) player.AttachedEntityUid, out IEntity playerEntity) && playerEntity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    OnExitRange(player, overlayEffects);
                }
            }
        }

        public void OnTick()
        {
            List<IPlayerSession> players = PlayerManager.GetPlayersInRange(Owner.Transform.MapPosition, Radius);
            foreach (var player in players) {
                if (!ActivatedPlayers.Contains(player))
                { 
                    if (player.AttachedEntityUid != null && EntityManager.TryGetEntity((EntityUid)player.AttachedEntityUid, out IEntity playerEntity) && playerEntity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                    {
                        ActivatedPlayers.Add(player);
                        OnEnterRange(player, overlayEffects);
                    }
                }
            }
            for (int i = 0; i < ActivatedPlayers.Count; i++)
            {
                var player = ActivatedPlayers[i];
                if (!players.Contains(player))
                {
                    ActivatedPlayers.Remove(player);
                    i--;
                    if (player.AttachedEntityUid != null && EntityManager.TryGetEntity((EntityUid)player.AttachedEntityUid, out IEntity playerEntity) && playerEntity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                    {
                        OnExitRange(player, overlayEffects);
                    }
                }
            }
            TickBehavior();
        }

        protected virtual void TickBehavior() { }

        protected virtual void OnEnterRange(IPlayerSession session, ServerOverlayEffectsComponent overlayEffects){ }

        protected virtual void OnExitRange(IPlayerSession session, ServerOverlayEffectsComponent overlayEffects) { }
    }
}
