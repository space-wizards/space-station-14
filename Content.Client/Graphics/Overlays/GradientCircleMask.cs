using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace Content.Client.Graphics.Overlays
{
    public class GradientCircleMask : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEyeManager _eyeManager;


        public GradientCircleMask() : base(nameof(GradientCircleMask))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("gradientcirclemask").Instance();
        }

        protected override void Draw(DrawingHandle handle)
        {
            var worldHandle = (DrawingHandleScreen)handle;
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.DrawRect(new UIBox2(_eyeManager.WorldToScreen(viewport.TopLeft), _eyeManager.WorldToScreen(viewport.BottomRight)), Color.White);
        }
    }
}
