using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace Content.Client.Graphics.Overlays
{
    public class CircleMaskOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEyeManager _eyeManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public CircleMaskOverlay() : base(nameof(CircleMaskOverlay))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("circlemask").Instance();
        }

        protected override void Draw(DrawingHandle handle)
        {
            var worldHandle = (DrawingHandleWorld)handle;
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
