using System.Collections.Generic;
using System.Linq;
using Content.Shared.Singularity.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Singularity
{
    public class SingularityOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const float MaxDist = 15.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _shader;
        private readonly Dictionary<EntityUid, SingularityShaderInstance> _singularities = new();

        public SingularityOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Singularity").Instance().Duplicate();
        }

        public override bool OverwriteTargetFrameBuffer()
        {
            return _singularities.Count > 0;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            SingularityQuery(args.Viewport.Eye);

            var viewportWB = args.WorldBounds;
            // Has to be correctly handled because of the way intensity/falloff transform works so just do it.
            _shader?.SetParameter("renderScale", args.Viewport.RenderScale);
            foreach (SingularityShaderInstance instance in _singularities.Values)
            {
                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(instance.CurrentMapCoords);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;
                _shader?.SetParameter("positionInput", tempCoords);
                if (ScreenTexture != null)
                    _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                _shader?.SetParameter("intensity", instance.Intensity);
                _shader?.SetParameter("falloff", instance.Falloff);

                var worldHandle = args.WorldHandle;
                worldHandle.UseShader(_shader);
                worldHandle.DrawRect(viewportWB, Color.White);
            }

        }

        //Queries all singulos on the map and either adds or removes them from the list of rendered singulos based on whether they should be drawn (in range? on the same z-level/map? singulo entity still exists?)
        private void SingularityQuery(IEye? currentEye)
        {
            if (currentEye == null)
            {
                _singularities.Clear();
                return;
            }

            var currentEyeLoc = currentEye.Position;

            var distortions = _entityManager.EntityQuery<SingularityDistortionComponent>();
            foreach (var distortion in distortions) //Add all singulos that are not added yet but qualify
            {
                var singuloEntity = distortion.Owner;

                if (!_singularities.Keys.Contains(singuloEntity.Uid) && SinguloQualifies(singuloEntity, currentEyeLoc))
                {
                    _singularities.Add(singuloEntity.Uid, new SingularityShaderInstance(singuloEntity.Transform.MapPosition.Position, distortion.Intensity, distortion.Falloff));
                }
            }

            var activeShaderIds = _singularities.Keys;
            foreach (var activeSinguloUid in activeShaderIds) //Remove all singulos that are added and no longer qualify
            {
                if (_entityManager.TryGetEntity(activeSinguloUid, out var singuloEntity))
                {
                    if (!SinguloQualifies(singuloEntity, currentEyeLoc))
                    {
                        _singularities.Remove(activeSinguloUid);
                    }
                    else
                    {
                        if (!singuloEntity.TryGetComponent<SingularityDistortionComponent>(out var distortion))
                        {
                            _singularities.Remove(activeSinguloUid);
                        }
                        else
                        {
                            var shaderInstance = _singularities[activeSinguloUid];
                            shaderInstance.CurrentMapCoords = singuloEntity.Transform.MapPosition.Position;
                            shaderInstance.Intensity = distortion.Intensity;
                            shaderInstance.Falloff = distortion.Falloff;
                        }
                    }

                }
                else
                {
                    _singularities.Remove(activeSinguloUid);
                }
            }

        }

        private bool SinguloQualifies(IEntity singuloEntity, MapCoordinates currentEyeLoc)
        {
            return singuloEntity.Transform.MapID == currentEyeLoc.MapId && singuloEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, singuloEntity.Transform.ParentUid, currentEyeLoc), MaxDist);
        }

        private sealed class SingularityShaderInstance
        {
            public Vector2 CurrentMapCoords;
            public float Intensity;
            public float Falloff;

            public SingularityShaderInstance(Vector2 mapCoords, float intensity, float falloff)
            {
                CurrentMapCoords = mapCoords;
                Intensity = intensity;
                Falloff = falloff;
            }
        }
    }
}

