using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.StationEvents
{
    public class RadiationPulseOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const float MaxDist = 15.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private TimeSpan _lastTick = default;

        private readonly ShaderInstance _shader;
        private readonly Dictionary<EntityUid, RadiationShaderInstance> _pulses = new();

        public RadiationPulseOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Radiation").Instance().Duplicate();
        }

        /* public override bool OverwriteTargetFrameBuffer() */
        /* { */
        /*     return _pulses.Count > 0; */
        /* } */

        protected override void Draw(in OverlayDrawArgs args)
        {
            RadiationQuery(args.Viewport.Eye);

            var viewportWB = _eyeManager.GetWorldViewport();
            // Has to be correctly handled because of the way intensity/falloff transform works so just do it.
            _shader?.SetParameter("renderScale", args.Viewport.RenderScale);
            foreach (RadiationShaderInstance instance in _pulses.Values)
            {
                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(instance.CurrentMapCoords);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;
                _shader?.SetParameter("positionInput", tempCoords);
                _shader?.SetParameter("range", instance.Range);
                _shader?.SetParameter("life", ((float) _lastTick.TotalSeconds - instance.Start) / (instance.End / instance.Start));
                if (ScreenTexture != null)
                    _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

                var worldHandle = args.WorldHandle;
                worldHandle.UseShader(_shader);
                /* worldHandle.DrawCircle(instance.CurrentMapCoords, instance.Range + 2.0f, Color.White); */
                worldHandle.DrawRect(viewportWB, Color.White);
            }
        }

        //Queries all pulses on the map and either adds or removes them from the list of rendered pulses based on whether they should be drawn (in range? on the same z-level/map? pulse entity still exists?)
        private void RadiationQuery(IEye? currentEye)
        {
            if (currentEye == null)
            {
                _pulses.Clear();
                return;
            }

            _lastTick = _gameTiming.CurTime;

            var currentEyeLoc = currentEye.Position;

            var pulses = _entityManager.EntityQuery<RadiationPulseComponent>();
            foreach (var pulse in pulses) //Add all pulses that are not added yet but qualify
            {
                var pulseEntity = pulse.Owner;

                if (!_pulses.Keys.Contains(pulseEntity.Uid) && PulseQualifies(pulseEntity, currentEyeLoc))
                {
                    _pulses.Add(
                            pulseEntity.Uid,
                            new RadiationShaderInstance(
                                pulseEntity.Transform.MapPosition.Position,
                                pulse.Range,
                                (float) _lastTick.TotalSeconds,
                                (float) pulse.EndTime.TotalSeconds
                            )
                    );
                }
            }

            var activeShaderIds = _pulses.Keys;
            foreach (var activePulseUid in activeShaderIds) //Remove all pulses that are added and no longer qualify
            {
                if (_entityManager.TryGetEntity(activePulseUid, out var pulseEntity) &&
                    PulseQualifies(pulseEntity, currentEyeLoc) &&
                    pulseEntity.TryGetComponent<RadiationPulseComponent>(out var pulse))
                {
                    var shaderInstance = _pulses[activePulseUid];
                    shaderInstance.CurrentMapCoords = pulseEntity.Transform.MapPosition.Position;
                    shaderInstance.Range = pulse.Range;
                } else {
                    _pulses.Remove(activePulseUid);
                }
            }

        }

        private bool PulseQualifies(IEntity pulseEntity, MapCoordinates currentEyeLoc)
        {
            return pulseEntity.Transform.MapID == currentEyeLoc.MapId && pulseEntity.Transform.Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, pulseEntity.Transform.ParentUid, currentEyeLoc), MaxDist);
        }

        private sealed record RadiationShaderInstance(Vector2 CurrentMapCoords, float Range, float Start, float End)
        {
            public Vector2 CurrentMapCoords = CurrentMapCoords;
            public float Range = Range;
            public float Start = Start;
            public float End = End;
        };
    }
}

