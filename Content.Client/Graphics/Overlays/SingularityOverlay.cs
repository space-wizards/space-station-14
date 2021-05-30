#nullable enable
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using Robust.Client.Graphics;
using System.Linq;
using System;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Content.Client.GameObjects.Components.Singularity;
using Robust.Shared.Map;

namespace Content.Client.Graphics.Overlays
{
    public class SingularityOverlay : Overlay
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _shader;

        Dictionary<EntityUid, SingularityShaderInstance> _singularities = new Dictionary<EntityUid, SingularityShaderInstance>();

        public SingularityOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Singularity").Instance().Duplicate();
        }

        public override bool OverwriteTargetFrameBuffer()
        {
            return _singularities.Count() > 0;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            SingularityQuery(args.Viewport.Eye);

            var viewportWB = args.WorldBounds;
            // This is a blatant cheat.
            // The correct way of doing this would be if the singularity shader performed the matrix transforms.
            // I don't need to explain why I'm not doing that.
            var resolution = Math.Max(0.125f, Math.Min(args.Viewport.RenderScale.X, args.Viewport.RenderScale.Y));
            foreach (SingularityShaderInstance instance in _singularities.Values)
            {
                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(instance.CurrentMapCoords);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;
                _shader?.SetParameter("positionInput", tempCoords);
                if (ScreenTexture != null)
                    _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                _shader?.SetParameter("intensity", instance.Intensity / resolution);
                _shader?.SetParameter("falloff", instance.Falloff / resolution);

                var worldHandle = args.WorldHandle;
                worldHandle.UseShader(_shader);
                worldHandle.DrawRect(viewportWB, Color.White);
            }

        }



        //Queries all singulos on the map and either adds or removes them from the list of rendered singulos based on whether they should be drawn (in range? on the same z-level/map? singulo entity still exists?)
        private float _maxDist = 15.0f;
        private void SingularityQuery(IEye? currentEye)
        {
            if (currentEye == null)
            {
                _singularities.Clear();
                return;
            }
            var currentEyeLoc = currentEye.Position;
            var currentMap = currentEye.Position.MapId;

            var singuloComponents = _componentManager.EntityQuery<IClientSingularityInstance>();
            foreach (var singuloInterface in singuloComponents) //Add all singulos that are not added yet but qualify
            {
                var singuloComponent = (Component)singuloInterface;
                var singuloEntity = singuloComponent.Owner;
                if (!_singularities.Keys.Contains(singuloEntity.Uid) && SinguloQualifies(singuloEntity, currentEyeLoc))
                {
                    _singularities.Add(singuloEntity.Uid, new SingularityShaderInstance(singuloEntity.Transform.MapPosition.Position, singuloInterface.Intensity, singuloInterface.Falloff));
                }
            }

            var activeShaderUids = _singularities.Keys;
            foreach (var activeSinguloUid in activeShaderUids) //Remove all singulos that are added and no longer qualify
            {
                if (_entityManager.TryGetEntity(activeSinguloUid, out IEntity? singuloEntity))
                {
                    if (!SinguloQualifies(singuloEntity, currentEyeLoc))
                    {
                        _singularities.Remove(activeSinguloUid);
                    }
                    else
                    {
                        if (!singuloEntity.TryGetComponent<IClientSingularityInstance>(out var singuloInterface))
                        {
                            _singularities.Remove(activeSinguloUid);
                        }
                        else
                        {
                            var shaderInstance = _singularities[activeSinguloUid];
                            shaderInstance.CurrentMapCoords = singuloEntity.Transform.MapPosition.Position;
                            shaderInstance.Intensity = singuloInterface.Intensity;
                            shaderInstance.Falloff = singuloInterface.Falloff;
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
            return singuloEntity.Transform.MapID == currentEyeLoc.MapId && singuloEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, singuloEntity.Transform.ParentUid, currentEyeLoc), _maxDist);
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

