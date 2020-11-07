using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Effects
{
   
    /// <summary>
    ///     Gravitational lensing shader application to all people who can see the singularity.
    /// </summary>
    [RegisterComponent]
    public class SingularityShaderAuraComponent : BaseShaderAuraComponent
    {
        public override string Name => "SingularityShaderAura";

        protected override int Radius => 9;
        protected string CurrentActiveTexturePath = "Objects/Fun/toys.rsi";
        protected string CurrentActiveTextureState = "singularitytoy";

        private Dictionary<IPlayerSession, IDContainer> _sessionToOverlays = new Dictionary<IPlayerSession, IDContainer>();

        protected override void OnEnterRange(IPlayerSession session, ServerOverlayEffectsComponent overlayEffects)
        {
            if (Owner.TryGetComponent<ITransformComponent>(out var transform)){
                Guid lensingOverlay, textureOverlay;
                lensingOverlay = overlayEffects.AddNewOverlay(OverlayType.SingularityOverlay,
                    new OverlayParameter[] {
                        new PositionOverlayParameter(transform.WorldPosition)
                    }
                );
                textureOverlay = overlayEffects.AddNewOverlay(OverlayType.TextureOverlay,
                    new OverlayParameter[] {
                        new OverlaySpaceOverlayParameter(OverlaySpace.WorldSpaceFOVStencil),
                        new PositionOverlayParameter(transform.WorldPosition),
                        new TextureOverlayParameter(CurrentActiveTexturePath, CurrentActiveTextureState)
                    }
                );
                _sessionToOverlays.Add(session, new IDContainer(lensingOverlay, textureOverlay));
            }
        }

        protected override void OnExitRange(IPlayerSession session, ServerOverlayEffectsComponent overlayEffects)
        {
            if (_sessionToOverlays.TryGetValue(session, out IDContainer ids))
            {
                overlayEffects.TryRemoveOverlay(ids.SingularityOverlayID);
                overlayEffects.TryRemoveOverlay(ids.TextureOverlayID);
                _sessionToOverlays.Remove(session);
            }
        }

        protected override void TickBehavior()
        {
            foreach (var player in ActivatedPlayers) {
                if (player.AttachedEntityUid != null && EntityManager.TryGetEntity((EntityUid) player.AttachedEntityUid, out IEntity playerEntity) && playerEntity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    if (Owner.TryGetComponent<ITransformComponent>(out var transform) && _sessionToOverlays.TryGetValue(player, out IDContainer ids))
                    {
                        overlayEffects.TryModifyOverlay(ids.SingularityOverlayID, overlay =>
                        {
                            if (overlay.TryGetOverlayParameter<PositionOverlayParameter>(out var pos))
                            {
                                pos.Positions = new Vector2[] { transform.WorldPosition };
                            }
                        });
                        overlayEffects.TryModifyOverlay(ids.TextureOverlayID, overlay => {
                            if (overlay.TryGetOverlayParameter<PositionOverlayParameter>(out var pos))
                            {
                                pos.Positions = new Vector2[] { transform.WorldPosition };
                            }
                        });
                    }
                }
            }
        }

        public void ChangeActiveSingularityTexture(string newPath)
        {
            CurrentActiveTexturePath = newPath;
        }

        private struct IDContainer
        {
            public Guid SingularityOverlayID;
            public Guid TextureOverlayID;

            public IDContainer(Guid singularityOverlayID, Guid textureOverlayID)
            {
                SingularityOverlayID = singularityOverlayID;
                TextureOverlayID = textureOverlayID;
            }
        }
    }
}
