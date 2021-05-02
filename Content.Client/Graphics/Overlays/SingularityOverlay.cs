#nullable enable
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using Robust.Client.Graphics;
using System.Linq;
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
        [Dependency] private readonly IEyeManager _eyeManager = default!;
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
            SingularityQuery();

            foreach (SingularityShaderInstance instance in _singularities.Values)
            {
                var tempCoords = _eyeManager.WorldToScreen(instance.CurrentMapCoords);
                tempCoords.Y = _displayManager.ScreenSize.Y - tempCoords.Y;
                _shader?.SetParameter("positionInput", tempCoords);
                if (ScreenTexture != null)
                    _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                _shader?.SetParameter("intensity", LevelToIntensity(instance.Level));
                _shader?.SetParameter("falloff", LevelToFalloff(instance.Level));

                var worldHandle = args.WorldHandle;
                worldHandle.UseShader(_shader);
                var viewport = _eyeManager.GetWorldViewport();
                worldHandle.DrawRect(viewport, Color.White);
            }

        }



        //Queries all singulos on the map and either adds or removes them from the list of rendered singulos based on whether they should be drawn (in range? on the same z-level/map? singulo entity still exists?)
        private float _maxDist = 15.0f;
        private void SingularityQuery()
        {
            var currentEyeLoc = _eyeManager.CurrentEye.Position;
            var currentMap = _eyeManager.CurrentMap; //TODO: support multiple viewports once it is added

            var singuloComponents = _componentManager.EntityQuery<IClientSingularityInstance>();
            foreach (var singuloInterface in singuloComponents) //Add all singulos that are not added yet but qualify
            {
                var singuloComponent = (Component)singuloInterface;
                var singuloEntity = singuloComponent.Owner;
                if (!_singularities.Keys.Contains(singuloEntity.Uid) && singuloEntity.Transform.MapID == currentMap && singuloEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, singuloEntity.Transform.ParentUid, currentEyeLoc), _maxDist))
                {
                    _singularities.Add(singuloEntity.Uid, new SingularityShaderInstance(singuloEntity.Transform.MapPosition.Position, singuloInterface.Level));
                }
            }

            var activeShaderUids = _singularities.Keys;
            foreach (var activeSinguloUid in activeShaderUids) //Remove all singulos that are added and no longer qualify
            {
                if (_entityManager.TryGetEntity(activeSinguloUid, out IEntity? singuloEntity))
                {
                    if (singuloEntity.Transform.MapID != currentMap || !singuloEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, singuloEntity.Transform.ParentUid, currentEyeLoc), _maxDist))
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
                            shaderInstance.Level = singuloInterface.Level;
                        }
                    }

                }
                else
                {
                    _singularities.Remove(activeSinguloUid);
                }
            }

        }




        //I am lazy
        private float LevelToIntensity(int level)
        {
            switch (level)
            {
                case 0:
                    return 0.0f;
                case 1:
                    return 2.7f;
                case 2:
                    return 14.4f;
                case 3:
                    return 47.2f;
                case 4:
                    return 180.0f;
                case 5:
                    return 600.0f;
                case 6:
                    return 800.0f;

            }
            return -1.0f;
        }
        private float LevelToFalloff(int level)
        {
            switch (level)
            {
                case 0:
                    return 9999f;
                case 1:
                    return 6.4f;
                case 2:
                    return 7.0f;
                case 3:
                    return 8.0f;
                case 4:
                    return 10.0f;
                case 5:
                    return 12.0f;
                case 6:
                    return 12.0f;
            }
            return -1.0f;
        }

        private sealed class SingularityShaderInstance
        {
            public Vector2 CurrentMapCoords;
            public int Level;
            public SingularityShaderInstance(Vector2 mapCoords, int level)
            {
                CurrentMapCoords = mapCoords;
                Level = level;
            }
        }
    }
}

