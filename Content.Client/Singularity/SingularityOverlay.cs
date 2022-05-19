using Content.Shared.Singularity.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Singularity
{
    public sealed class SingularityOverlay : Overlay
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
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null || args.Viewport.Eye == null)
                return;

            // Has to be correctly handled because of the way intensity/falloff transform works so just do it.
            _shader?.SetParameter("renderScale", args.Viewport.RenderScale);

            var position = new Vector2[MaxCount];
            var intensity = new float[MaxCount];
            var falloffPower = new float[MaxCount];
            int count = 0;

            var mapId = args.Viewport.Eye.Position.MapId;

            foreach (var distortion in _entMan.EntityQuery<SingularityDistortionComponent>())
            {
                var mapPos = _entMan.GetComponent<TransformComponent>(distortion.Owner).MapPosition;
                if (mapPos.MapId != mapId)
                    continue;

                // is the distortion in range?
                if ((mapPos.Position - args.WorldAABB.ClosestPoint(mapPos.Position)).LengthSquared > MaxDistance * MaxDistance)
                    continue;

                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(mapPos.Position);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

                position[count] = tempCoords;
                intensity[count] = distortion.Intensity;
                falloffPower[count] = distortion.FalloffPower;
                count++;

                if (count == MaxCount)
                    break;
            }

            if (count == 0)
                return;

            _shader?.SetParameter("count", count);
            _shader?.SetParameter("position", position);
            _shader?.SetParameter("intensity", intensity);
            _shader?.SetParameter("falloffPower", falloffPower);
            _shader?.SetParameter("maxDistance", MaxDistance * EyeManager.PixelsPerMeter);
            _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(args.WorldAABB, Color.White);
            worldHandle.UseShader(null);
        }
    }
}

