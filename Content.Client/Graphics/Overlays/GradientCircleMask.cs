using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Graphics.Overlays
{
    public class GradientCircleMask : Overlay
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public GradientCircleMask() : base(nameof(GradientCircleMask))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            var worldHandle = (DrawingHandleWorld)handle;
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
