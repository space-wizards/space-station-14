using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Client.GameObjects;
using Content.Client.Viewport;

namespace Content.Client.Camera
{
    public class CameraSystem : EntitySystem
    {
        public ScalingViewport CreateCameraViewport(Vector2i viewportSize,EyeComponent eyecomponent,ScalingViewportRenderScaleMode scalingviewportrenderscalemode)
        {
            return new ScalingViewport
            {
                ViewportSize = viewportSize,
                Eye = eyecomponent.Eye,
                RenderScaleMode = scalingviewportrenderscalemode,
            };
        }
    }
}