using Content.Client.Viewport;
using Content.Shared.Singularity.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Singularity
{
    public sealed class SingularityOverlay : Overlay, IEntityEventSubscriber
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     Maximum number of distortions that can be shown on screen at a time.
        ///     If this value is changed, the shader itself also needs to be updated.
        /// </summary>
        public const int MaxCount = 5;

        private const float MaxDistance = 20f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _shader;

        public SingularityOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Singularity").Instance().Duplicate();
            _shader.SetParameter("maxDistance", MaxDistance * EyeManager.PixelsPerMeter);
            _entMan.EventBus.SubscribeEvent<ProjectScreenToMapEvent>(EventSource.Local, this, OnProjectFromScreenToMap);
        }

        private Vector2[] _positions = new Vector2[MaxCount];
        private float[] _intensities = new float[MaxCount];
        private float[] _falloffPowers = new float[MaxCount];
        private int _count = 0;

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (args.Viewport.Eye == null)
                return false;

            _count = 0;
            foreach (var (distortion, xform) in _entMan.EntityQuery<SingularityDistortionComponent, TransformComponent>())
            {
                if (xform.MapID != args.MapId)
                    continue;

                var mapPos = xform.WorldPosition;

                // is the distortion in range?
                if ((mapPos - args.WorldAABB.ClosestPoint(mapPos)).LengthSquared > MaxDistance * MaxDistance)
                    continue;

                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(mapPos);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

                _positions[_count] = tempCoords;
                _intensities[_count] = distortion.Intensity;
                _falloffPowers[_count] = distortion.FalloffPower;
                _count++;

                if (_count == MaxCount)
                    break;
            }

            return (_count > 0);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null || args.Viewport.Eye == null)
                return;

            _shader?.SetParameter("renderScale", args.Viewport.RenderScale);
            _shader?.SetParameter("count", _count);
            _shader?.SetParameter("position", _positions);
            _shader?.SetParameter("intensity", _intensities);
            _shader?.SetParameter("falloffPower", _falloffPowers);
            _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(args.WorldAABB, Color.White);
            worldHandle.UseShader(null);
        }

        private void OnProjectFromScreenToMap(ref ProjectScreenToMapEvent args)
        {   // Mostly copypasta from the singularity shader.
            Vector2 finalCoords = args.ScreenPosition;
            Vector2 delta;
            float distance = 0.0f;
            float deformation = 0.0f;
            float maxDistance = MaxDistance * EyeManager.PixelsPerMeter;
    
            for (int i = 0; i < MaxCount && i < _count; i++)
            {
                delta = args.ScreenPosition - _positions[i];
                distance = (delta / args.ClydeViewport.RenderScale).Length;

                deformation = _intensities[i] / MathF.Pow(distance, _falloffPowers[i]);
                
                // ensure deformation goes to zero at max distance
                // avoids long-range single-pixel shifts that are noticeable when leaving PVS.
                
                if (distance >= maxDistance) {
                    deformation = 0.0f;
                } else {
                    deformation *= (1.0f - MathF.Pow(distance/maxDistance, 4.0f));
                }
                
                if(deformation > 0.8)
                    deformation = MathF.Pow(deformation, 0.3f);

                Vector2 displacement = delta * deformation;
                finalCoords -= displacement;
            }

            args.ScreenPosition = finalCoords;
        }
    }
}
