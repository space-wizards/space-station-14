using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Effects
{

    /// <summary>
    ///     Gravitational lensing shader application to all people who can see the singularity.
    /// </summary>
    [RegisterComponent]
    public class SingularityShaderAuraComponent : BaseShaderAuraComponent
    {
        public override string Name => "SingularityShaderAura";

        protected override int Radius => 25;
        protected string CurrentActiveTexturePath = "Objects/Fun/toys.rsi";
        protected string CurrentActiveTextureState = "singularitytoy";
        protected float CurrentIntensity = 3.8f;
        protected float CurrentFalloff = 5.1f;


        private Dictionary<IEntity, IDContainer> _entityToOverlays = new Dictionary<IEntity, IDContainer>();


        public void SetSingularityTexture(string newPath, string newState)
        {
            CurrentActiveTexturePath = newPath;
            CurrentActiveTextureState = newState;
            foreach (var entity in ActivatedEntities)
            {
                if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    if (_entityToOverlays.TryGetValue(entity, out IDContainer ids))
                    {
                        overlayEffects.TryModifyOverlay(ids.TextureOverlayID, overlay => {
                            if (overlay.TryGetOverlayParameter<TextureOverlayParameter>(out var texture))
                            {
                                texture.RSIPaths[0] = CurrentActiveTexturePath;
                                texture.States[0] = CurrentActiveTextureState;
                            }
                        });
                    }
                }
            }
        }

        public void SetEffectIntensity(float newIntensity, float newFalloff)
        {
            CurrentIntensity = newIntensity;
            CurrentFalloff = newFalloff;
            foreach (var entity in ActivatedEntities)
            {
                if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    if (_entityToOverlays.TryGetValue(entity, out IDContainer ids))
                    {
                        overlayEffects.TryModifyOverlay(ids.SingularityOverlayID, overlay => {
                            if (overlay.TryGetOverlayParameter<KeyedFloatOverlayParameter>(out var floatParam))
                            {
                                floatParam.SetValues(new Dictionary<string, float>() { { "intensity", CurrentIntensity }, { "falloff", CurrentFalloff } });
                            }
                        });
                    }
                }
            }
        }


        protected override void OnEnterRange(IEntity entity, ServerOverlayEffectsComponent overlayEffects)
        {
            if (Owner.TryGetComponent<ITransformComponent>(out var transform)){
                Guid lensingOverlay, textureOverlay;
                lensingOverlay = overlayEffects.AddNewOverlay(OverlayType.SingularityOverlay,
                    new OverlayParameter[] {
                        new KeyedVector2OverlayParameter(new Dictionary<string, Vector2>() { { "worldCoords", transform.WorldPosition } }),
                        new KeyedFloatOverlayParameter(new Dictionary<string, float>(){ { "intensity", CurrentIntensity }, { "falloff", CurrentFalloff } })
                    }
                );
                textureOverlay = overlayEffects.AddNewOverlay(OverlayType.TextureOverlay,
                    new OverlayParameter[] {
                        new KeyedOverlaySpaceOverlayParameter(new Dictionary<string, OverlaySpace>() { { "overlaySpace", OverlaySpace.WorldSpaceFOVStencil } }),
                        new KeyedVector2OverlayParameter(new Dictionary<string, Vector2>() { { "worldCoords", transform.WorldPosition } }),
                        new TextureOverlayParameter(CurrentActiveTexturePath, CurrentActiveTextureState),
                    }
                );
                _entityToOverlays.Add(entity, new IDContainer(lensingOverlay, textureOverlay));
            }
        }

        protected override void OnExitRange(IEntity entity, ServerOverlayEffectsComponent overlayEffects)
        {
            if (_entityToOverlays.TryGetValue(entity, out IDContainer ids))
            {
                overlayEffects.TryRemoveOverlay(ids.SingularityOverlayID);
                overlayEffects.TryRemoveOverlay(ids.TextureOverlayID);
                _entityToOverlays.Remove(entity);
            }
        }

        protected override void TickBehavior()
        {
            foreach (var entity in ActivatedEntities) {
                if (entity.TryGetComponent<ServerOverlayEffectsComponent>(out ServerOverlayEffectsComponent overlayEffects))
                {
                    if (Owner.TryGetComponent<ITransformComponent>(out var transform) && _entityToOverlays.TryGetValue(entity, out IDContainer ids))
                    {
                        overlayEffects.TryModifyOverlay(ids.SingularityOverlayID, overlay =>
                        {
                            if (overlay.TryGetOverlayParameter<KeyedVector2OverlayParameter>(out var posParam))
                            {
                                posParam.SetValues(new Dictionary<string, Vector2>() { { "worldCoords", transform.WorldPosition } });
                            }
                        });
                        overlayEffects.TryModifyOverlay(ids.TextureOverlayID, overlay => {
                            if (overlay.TryGetOverlayParameter<KeyedVector2OverlayParameter>(out var posParam))
                            {
                                posParam.SetValues(new Dictionary<string, Vector2>() { { "worldCoords", transform.WorldPosition } });
                            }
                        });
                    }
                }
            }
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
